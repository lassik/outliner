﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;
using System.Windows.Forms;
using System.IO;

namespace Outliner
{
    static class CalendarOps
    {
        private static int DayOfWeekISO(DateTime dt)
        {
            var wd = (int)dt.DayOfWeek;
            if (wd == 0) { return 7; }
            return wd;
        }

        private static int WeekOfYearISO(DateTime dt)
        {
            var nearestThursday = dt.AddDays(4 - DayOfWeekISO(dt));
            var january1 = new DateTime(nearestThursday.Year, 1, 1);
            return nearestThursday.Subtract(january1).Days / 7 + 1;
        }

        private static DateTime ParseDate(string yearStr, string monthStr,
            string dayStr)
        {
            try
            {
                return new DateTime(
                    Convert.ToInt32(yearStr),
                    Convert.ToInt32(monthStr),
                    Convert.ToInt32(dayStr));
            }
            catch (ArgumentOutOfRangeException) { }
            return DateTime.MinValue;
        }

        private class ParsedEvent
        {
            public bool HasDate { get { return Date > DateTime.MinValue; } }

            public bool HasTime { get { return Time != null; } }

            public DateTime Date { get; private set; }

            public string Time { get; private set; }

            public string GivenColor { get; private set; }

            public string Text { get; private set; }

            public ParsedEvent(string str)
            {
                Match match;
                Text = str;

                match = Regex.Match(Text, @"^(\d{4})-(\d{2})-(\d{2}) +(.*)$");
                if (match.Success)
                {
                    Date = ParseDate(
                        match.Groups[1].Value,
                        match.Groups[2].Value,
                        match.Groups[3].Value);
                    if (HasDate) { Text = match.Groups[4].Value; }
                }

                match = Regex.Match(Text, @"^(\d[\d:-]+) +(.*)$");
                if (match.Success)
                {
                    Time = match.Groups[1].Value;
                    Text = match.Groups[2].Value;
                }

                match = Regex.Match(Text, @"^(.*) *\[(.*?)\]$");
                if (match.Success)
                {
                    GivenColor = match.Groups[2].Value;
                    Text = match.Groups[1].Value;
                }
            }

            public string Color
            {
                get
                {
                    if (HasDate && (Date < DateTime.Today)) { return "gray"; }
                    if (GivenColor != null) { return GivenColor; }
                    return "black";
                }
            }
        }

        private class ParsedEventSorter : IComparer<ParsedEvent>
        {
            private static readonly CaseInsensitiveComparer strcmp =
                new CaseInsensitiveComparer();

            public int Compare(ParsedEvent a, ParsedEvent b)
            {
                int cmp;

                cmp = ((a == null) ? 0 : 1).CompareTo((b == null) ? 0 : 1);
                if (cmp != 0) { return cmp; }

                cmp = a.Date.CompareTo(b.Date);  // Relies on no date being MinValue
                if (cmp != 0) { return cmp; }

                cmp = (a.HasTime ? a.Time : "").CompareTo(b.HasTime ? b.Time : "");
                if (cmp != 0) { return cmp; }

                cmp = strcmp.Compare(a.Time, b.Time);
                return cmp;
            }

            private ParsedEventSorter() { }

            public static readonly ParsedEventSorter Instance = new ParsedEventSorter();
        }

        private static string ParsedEventHTML(ParsedEvent evt)
        {
            var htime = evt.HasTime ? "<b>" + evt.Time + "</b> " : "";
            return String.Format("<font color={0}>&bull; {1}{2}</font><br>",
                evt.Color, htime, evt.Text);
        }

        private static string ColorfulDateHTML(DateTime date)
        {
            return "<center><b>" +
                String.Format("<font color=gray>{0}</font>", date.Year) +
                String.Format("<font color=navy>{0:00}</font>", date.Month) +
                String.Format("<font color=red>{0:00}</font>", date.Day) +
                "</b></center>";
        }

        private static Dictionary<DateTime, List<ParsedEvent>>
            BuildDayToEventsTable(TreeNodeCollection nodes)
        {
            var dayToEvents = new Dictionary<DateTime, List<ParsedEvent>>();

            Util.EachNode(nodes,
                delegate(TreeNode node)
                {
                    var evt = new ParsedEvent(node.Text);
                    if (evt.HasDate)
                    {
                        if (dayToEvents[evt.Date] == null)
                        {
                            dayToEvents[evt.Date] = new List<ParsedEvent>();
                        }
                        dayToEvents[evt.Date].Add(evt);
                    }
                });

            foreach (var date in dayToEvents.Keys)
            {
                dayToEvents[date].Sort(ParsedEventSorter.Instance);
            }
            return dayToEvents;
        }

        private static string DayEventsHTML(Dictionary<DateTime, List<ParsedEvent>> dayToEvents, DateTime day)
        {
            var html = new StringBuilder();
            List<ParsedEvent> events;
            if (dayToEvents.TryGetValue(day, out events))
            {
                foreach (var evt in events)
                {
                    html.Append(ParsedEventHTML(evt));
                }
            }
            return html.ToString();
        }

        private static void DumpCalendarWeeks(Dictionary<DateTime, List<ParsedEvent>> dayToEvents,
            DateTime today, int weekCount, StreamWriter output)
        {
            output.Write("<table border=1 width=100% height=100%>");

            output.Write("<tr><th><font color=red>&hearts;</font></th>");
            for (var weekday = 1; weekday <= 7; weekday++)
            {
                output.Write(String.Format("<th width=14%>{0}</th>", weekday));
            }
            output.Write("</tr>");

            var day = today.Date;
            day = day.AddDays(-DayOfWeekISO(day) + 1);
            while (weekCount > 0)
            {
                output.Write(String.Format("<tr height=200><th>W{0:00}</th>", WeekOfYearISO(day)));
                for (var weekday = 1; weekday <= 7; weekday++)
                {
                    output.Write("<td valign=top>" + ColorfulDateHTML(day) +
                        DayEventsHTML(dayToEvents, day) + "</td>");
                    day = day.AddDays(1);
                }
                output.Write("</tr>");
                weekCount--;
            }

            output.Write("</table>");
        }

        public static void DumpCalendarAsHTML(TreeNodeCollection nodes, string htmlFilename)
        {
            using (var output = File.CreateText(htmlFilename))
            {
                output.Write("<title>Calendar</title>");
                output.Write("<meta http-equiv=\"content-type\" content=\"text/html; charset=utf-8\">");
                output.Write("<body style=\"font-family: sans-serif;\">");
                DumpCalendarWeeks(BuildDayToEventsTable(nodes), DateTime.Today, 12, output);
            }
        }
    }
}
