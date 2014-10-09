using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Outliner
{
    class CalendarOps
    {
        int DayOfWeekISO(DateTime DT)
        {
            int WD = (int)DT.DayOfWeek;
            if (WD == 0)
            {
                return 7;
            }
            return WD;
        }

        int WeekOfYearISO(DateTime DT)
        {
            DateTime NearestThu = DT.AddDays(4 - DayOfWeekISO(DT));
            DateTime Jan1 = new DateTime(NearestThu.Year, 1, 1);
            return NearestThu.Subtract(Jan1).Days / 7 + 1;
        }

        static DateTime DateNow
        {
            get
            {
                DateTime dt = DateTime.Now;
                return new DateTime(dt.Year, dt.Month, dt.Day);
            }
        }

        static bool ParseDate(ref DateTime date, string ys, string ms, string ds)
        {
            try
            {
                date = new DateTime(Convert.ToInt32(ys),
                                    Convert.ToInt32(ms),
                                    Convert.ToInt32(ds));
                return true;
            }
            catch (ArgumentOutOfRangeException exception)
            {
                return false;
            }
        }

        class ParsedEvent
        {
            public bool HasDate;

            public bool HasTime
            {
                get
                {
                    return Time != null;
                }
            }

            public DateTime Date;

            public string Time;

            public string GivenColor;

            public string Text;

            public ParsedEvent(string s)
            {
                Text = s;
                Match match;
                match = Regex.Match(Text, @"^(\d{4})-(\d{2})-(\d{2}) +(.*)$");
                if (match.Success)
                {
                    HasDate = ParseDate(ref Date,
                                        match.Groups[1].Value,
                                        match.Groups[2].Value,
                                        match.Groups[3].Value);
                    if (HasDate)
                        Text = match.Groups[4].Value;
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
                    if (HasDate && (Date < DateNow))
                        return "gray";
                    if (GivenColor != null)
                        return GivenColor;
                    return "black";
                }
            }
        }

        private string EventHTML(ParsedEvent evt)
        {
            string htime = evt.HasTime ? "<b>" + evt.Time + "</b> " : "";
            return String.Format("<font color={0}>&bull; {1}{2}</font><br>", evt.Color, htime, evt.Text);
        }

        class DayEventSorter : IComparer
        {
            int IComparer.Compare(Object x, Object y)
            {
                ParsedEvent ex = (ParsedEvent)x;
                ParsedEvent ey = (ParsedEvent)y;
                if (ex == null || ey == null) return 0;
                //MessageBox.Show(String.Format("Comparing {0} and {1}", ex, ey));
                if (ex == ey) return 0;
                if (!ex.HasTime) return -1;
                if (!ey.HasTime) return 1;
                return new CaseInsensitiveComparer().Compare(ex.Time, ey.Time);
            }
        }

        string ColorfulDateHTML(DateTime date)
        {
            return "<center><b>" +
                String.Format("<font color=gray>{0}</font>", date.Year) +
                String.Format("<font color=navy>{0:00}</font>", date.Month) +
                String.Format("<font color=red>{0:00}</font>", date.Day) +
                "</b></center>";
        }

        Hashtable GetDayToEventsTable(TreeNodeCollection nodes)
        {
            Hashtable dayevts = new Hashtable();
            EachNode(nodes,
                     delegate(TreeNode node)
                     {
                         ParsedEvent evt = new ParsedEvent(node.Text);
                         if (evt.HasDate)
                         {
                             if (dayevts[evt.Date] == null)
                                 dayevts[evt.Date] = new ArrayList();
                             ((ArrayList)dayevts[evt.Date]).Add(evt);
                         }
                     });
            foreach (DateTime date in dayevts.Keys)
            {
                ((ArrayList)dayevts[date]).Sort(new DayEventSorter());
                //events.ToArray(typeof(ParsedEvent)) as ParsedEvent[]
            }
            return dayevts;
        }

        string DayEventsHTML(Hashtable dayevts, DateTime date)
        {
            string s = "";
            ArrayList evts = (ArrayList)dayevts[date];
            if (evts != null)
                foreach (ParsedEvent evt in evts)
                    s += EventHTML(evt);
            return s;
        }

        void DumpCalendarWeeks(StreamWriter Out, int nweeks, DateTime Day)
        {
            Hashtable dayevts = GetDayToEventsTable(tv.Nodes);
            Out.Write("<table border=1 width=100% height=100%>");
            Out.Write("<tr><th><font color=red>&hearts;</font></th>");
            for (int wd = 1; wd <= 7; wd++) Out.Write(String.Format("<th width=14%>{0}</th>", wd));
            Out.Write("</tr>");
            Day = new DateTime(Day.Year, Day.Month, Day.Day);
            Day = Day.AddDays(-DayOfWeekISO(Day) + 1);
            for (; nweeks > 0; nweeks--)
            {
                Out.Write(String.Format("<tr height=200><th>W{0:00}</th>", WeekOfYearISO(Day)));
                for (int wd = 1; wd <= 7; wd++)
                {
                    Out.Write(String.Format("<td valign=top>{0}{1}</td>", ColorfulDateHTML(Day), DayEventsHTML(dayevts, Day)));
                    Day = Day.AddDays(1);
                }
                Out.Write("</tr>");
            }
            Out.Write("</table>");
        }

        void cmdDumpCalendarAsHTML()
        {
            using (StreamWriter Out = File.CreateText(path + "cal.html"))
            {
                Out.Write("<title>Calendar</title>");
                Out.Write("<meta http-equiv=\"content-type\" content=\"text/html; charset=utf-8\">");
                Out.Write("<body style=\"font-family: sans-serif;\">");
                DumpCalendarWeeks(Out, 12, DateTime.Now);
            }
        }

    }
}
