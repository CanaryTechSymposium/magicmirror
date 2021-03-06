﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Graph;
using Windows.UI.Xaml;
using System.Threading.Tasks;
using System.Diagnostics;

namespace MagicMirror.Calendar.ExchangeProvider
{
    class ExchangeCalendarProvider
    {
        private ICalendarEventInterface _calendar;
        private DispatcherTimer _updateTimer;
        private TimeSpan _updateRate;


        public ExchangeCalendarProvider(ICalendarEventInterface calendar, TimeSpan updateRate)
        {
            _calendar = calendar;
            _updateRate = updateRate;

            StartTimer();
        }

        public void StartTimer()
        {
            if (_updateTimer != null)
                return;

            _updateTimer = new DispatcherTimer();
            _updateTimer.Interval = new TimeSpan(0, 0, 0, 0, 1);
            _updateTimer.Tick += TimerTick;
            _updateTimer.Start();
        }

        public void StopTimer()
        {
            if (_updateTimer != null)
            {
                _updateTimer.Stop();
                _updateTimer = null;
            }
        }

        private void TimerTick(object sender, object e)
        {
            _updateTimer.Stop();

            GetEventsAsync();
            //_calendar.SetCurrentEvents(GetCurrentEvents());
        }

        //public List<CalendarEvent> GetCurrentEvents()
        //{
        //    List<CalendarEvent> events = new List<MagicMirror.Calendar.CalendarEvent>();

        //    // Test data
        //    ////var start = new DateTime(2017, 3, 17, 11, 0, 0);
        //    ////var start = DateTime.Today.AddHours(11);
        //    //var start = DateTime.Now;
        //    //events.Add(new CalendarEvent { Description = "Tech Symposium", Start = start, End = start.AddHours(2) });
        //    //start = start.AddHours(3);
        //    //events.Add(new CalendarEvent { Description = "Clean up", Start = start, End = start.AddMinutes(30) });
        //    //start = start.AddDays(1).AddHours(4);
        //    //events.Add(new CalendarEvent { Description = "Party at Gary's", Start = start, End = start.AddHours(3) });
        //    //start = start.AddDays(2).AddHours(-6);
        //    //events.Add(new CalendarEvent { Description = "Finish potato chips", Start = start, End = start.AddHours(1) });

        //    //GetEventsAsync();
        //    //MagicMirror.Utilities.CredintialStore.LoadStore();
        //    //var start = DateTime.Now;
        //    //events.Add(new CalendarEvent { Description = MagicMirror.Utilities.CredintialStore.fileLocation, Start = start, End = start.AddHours(2) });


        //    return events;
        //}

        public async void GetEventsAsync()
        {
            List<CalendarEvent> events = new List<MagicMirror.Calendar.CalendarEvent>();

            try
            {
                var graphClient = AuthenticationHelper.GetAuthenticatedClient();

                // Define the time span for the calendar view.
                List<QueryOption> options = new List<QueryOption>();
                options.Add(new QueryOption("startDateTime", DateTime.Now.ToString("o")));
                options.Add(new QueryOption("endDateTime", DateTime.Now.AddDays(30).ToString("o")));
                var calendarUserId = MagicMirror.Utilities.CredintialStore.GetCredintials("calendar-calendarUserId");
                var myEvents = await graphClient.Users[calendarUserId.ID].CalendarView.Request(options).GetAsync();

                while (true)
                {
                    foreach (var item in myEvents)
                    {
                        if (item.IsCancelled.HasValue && item.IsCancelled.Value)
                            continue;

                        events.Add(new CalendarEvent { Description = item.Subject, Start = DateTime.Parse(item.Start.DateTime), End = DateTime.Parse(item.End.DateTime), IsAllDay = item.IsAllDay.HasValue ? item.IsAllDay.Value : false });
                    }

                    if (myEvents.NextPageRequest == null)
                        break;

                    myEvents = await myEvents.NextPageRequest.GetAsync();
                }
            }

            catch (ServiceException e)
            {
                events.Add(new CalendarEvent { Description = string.Format("Could not get company events: {0}", e.Error.Message), Start = DateTime.Now, End = DateTime.Now });
            }
            catch (Exception e)
            {
                events.Add(new CalendarEvent { Description = string.Format("Could not get company events: {0}", e.Message), Start = DateTime.Now, End = DateTime.Now });
            }

            // Callback into the calendar source.
            _calendar.SetCurrentEvents(events);

            // restart _updateTimer if it hasn't been stopped
            if (_updateTimer != null)
            {
                _updateTimer.Interval = _updateRate;
                _updateTimer.Start();
            }
        }
    }
}
