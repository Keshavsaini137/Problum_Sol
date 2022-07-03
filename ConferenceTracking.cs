using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;

public class EventData
{
    public string title;
    public int duration;
    public DateTime dateTime;

    public EventData(string title, int duration)
    {
        this.title = title;
        this.duration = duration;
    }

    public EventData(string title, DateTime dateTime)
    {
        this.title = title;
        this.dateTime = dateTime;
    }
}


public class Conference
{
    public List<Track> trackList;
}

public class Track
{
    public Session morning;
    public Session evening;
    public int index;
    public List<EventData> eventDataList;
}

public class Session
{
    public TimeSpan startTime;
    public TimeSpan endTime;

    public Session(TimeSpan startTime, TimeSpan endTime)
    {
        this.startTime = startTime;
        this.endTime = endTime;
    }
}

public enum DurationType { lightning }

public class ConferenceTracking
{
    static string[] testInputs = {
        "Rails for Python Developers lightning ",
        "Writing Fast Tests Against Enterprise Rails -  60min ",
        "Overdoing it in Python -  45min ",
        "Lua for the Masses -  30min ",
        "Ruby Errors from Mismatched Gem Versions - 45min ",
        "Common Ruby Errors - 45min ",
        "Rails for Python Developers lightning ",
        "Communicating Over Distance 60min ",
        "Accounting-Driven Development - 45min ",
        "Woah - 30min ",
        "Sit Down and Write - 30min ",
        "Pair Programming vs Noise - 45min ",
        "Rails Magic - 60min ",
        "Ruby on Rails: Why We Should Move On - 60min ",
        "Clojure Ate Scala (on my project) - 45min ",
        "Programming in the Boondocks of Seattle - 30min ",
        "Ruby vs. Clojure for Back-End Development - 30min ",
        "Ruby on Rails Legacy App Maintenance - 60min ",
        "A World Without HackerNews - 30min ",
        "User Interface CSS in Rails Apps - 30min"
    };

    static List<int> durationList = new List<int>();
    static Dictionary<int, List<EventData>> eventListByDuration = new Dictionary<int, List<EventData>>();

    public static Dictionary<DurationType, int> durationByType = new Dictionary<DurationType, int>()
{
    {DurationType.lightning, 5},
};

    public static void Main()
    {
        if (testInputs == null || testInputs.Length == 0) Console.WriteLine("Empty Input");

        int totalMins = 0;
        foreach (string title in testInputs)
        {
            string[] splitTitleList = title.Split("-", StringSplitOptions.TrimEntries);
            int whiteSpaceIndex = title.Trim().LastIndexOf(" ");
            if (whiteSpaceIndex == -1) continue; //ERROR : Invalid Title.
            string subString = title.Substring(whiteSpaceIndex).Trim();
            EventData eventData;

            if (subString.Equals(DurationType.lightning.ToString()))
            {
                eventData = new EventData(title, durationByType[DurationType.lightning]);
            }
            else
            {
                string resultString = Regex.Match(subString, @"\d+").Value;
                eventData = new EventData(String.Join(" ", splitTitleList), Int32.Parse(resultString));
            }
            durationList.Add(eventData.duration);
            totalMins += eventData.duration;
            //Console.WriteLine($"Event: {eventData.title} {eventData.duration}");
            List<EventData> events = null;
            eventListByDuration.TryGetValue(eventData.duration, out events);
            if (events == null) events = new List<EventData>();

            events.Add(eventData);
            eventListByDuration[eventData.duration] = events;
        }

        Track track = new Track();
        track.morning = new Session(new TimeSpan(9, 0, 0), new TimeSpan(12, 0, 0));
        track.evening = new Session(new TimeSpan(1, 0, 0), new TimeSpan(5, 0, 0));

        //get total mins in one track
        int morTotalMins = (int)(track.morning.endTime - track.morning.startTime).TotalMinutes;
        int eveTotalMins = (int)(track.evening.endTime - track.evening.startTime).TotalMinutes;
        int oneTrackMins = (morTotalMins + eveTotalMins);
        // Console.WriteLine($"oneTrackMins {oneTrackMins}");
        int numberOfTracks = totalMins / oneTrackMins;
        if (totalMins % oneTrackMins != 0) numberOfTracks++;

        Conference conference = new Conference();
        conference.trackList = new List<Track>();
        for (int i = 1; i <= numberOfTracks; i++)
        {
            Track tempTrack = new Track();
            tempTrack.index = i;
            tempTrack.eventDataList = new List<EventData>();

            //assign morning span
            tempTrack.eventDataList.AddRange(getSortedEventList(morTotalMins, track.morning.startTime));

            //assign Lunch
            EventData lunchEventData = new EventData("Lunch", new DateTime().AddHours(12));
            tempTrack.eventDataList.Add(lunchEventData);

            //assign Evening span
            tempTrack.eventDataList.AddRange(getSortedEventList(eveTotalMins, track.evening.startTime));

            //assign Network Event
            EventData netEventData = new EventData("Networking Event", new DateTime().AddHours(5));
            tempTrack.eventDataList.Add(netEventData);

            conference.trackList.Add(tempTrack);
        }

        foreach (Track track1 in conference.trackList)
        {
            Console.WriteLine($"Track: {track1.index}");
            foreach (EventData eventData in track1.eventDataList)
            {
                Console.WriteLine(eventData.dateTime.ToString("hh:mm tt") + " " + eventData.title);
            }
        }
    }

    private static List<EventData> getSortedEventList(int totalMins, TimeSpan startTime)
    {
        List<EventData> eventList = new List<EventData>();
        DateTime now = new DateTime();
        now = now.Add(startTime);
        List<int> morTracks = sum_up(durationList, totalMins);
        if (morTracks != null)
        {
            foreach (int trackNum in morTracks)
            {
                durationList.Remove(trackNum);
                List<EventData> events = eventListByDuration[trackNum];
                EventData eventData = events[events.Count - 1];
                events.RemoveAt(events.Count - 1);
                eventData.dateTime = now;
                eventList.Add(eventData);
                eventListByDuration[trackNum] = events;
                now = now.AddMinutes(eventData.duration);
            }
        }
        else
        {
            int tempMin = totalMins;
            while (durationList.Count > 0 && tempMin >= 0)
            {
                int trackNum = durationList[durationList.Count - 1];
                List<EventData> events = eventListByDuration[trackNum];
                EventData eventData = events[events.Count - 1];
                events.RemoveAt(events.Count - 1);
                eventData.dateTime = now;
                eventList.Add(eventData);
                eventListByDuration[trackNum] = events;
                now = now.AddMinutes(eventData.duration);

                durationList.Remove(trackNum);
                tempMin -= eventData.duration;
            }
        }
        return eventList;
    }

    private static List<int> sum_up(List<int> numbers, int target)
    {
        List<List<int>> ls = new List<List<int>>();
        sum_up_recursive(numbers, target, new List<int>(), ls);
        //Console.WriteLine("LS : " + ls.Count);
        if (ls.Count == 0) return null;
        else return ls[0];
    }

    private static void sum_up_recursive(List<int> numbers, int target, List<int> partial, List<List<int>> ls)
    {
        int s = 0;
        foreach (int x in partial) s += x;

        if (s == target)
        {
            //Console.WriteLine("sum(" + string.Join(",", partial.ToArray()) + ")=" + target);
            ls.Add(partial);
            return;
        }

        if (s >= target)
            return;

        for (int i = 0; i < numbers.Count; i++)
        {
            List<int> remaining = new List<int>();
            int n = numbers[i];
            for (int j = i + 1; j < numbers.Count; j++) remaining.Add(numbers[j]);

            List<int> partial_rec = new List<int>(partial);
            partial_rec.Add(n);
            sum_up_recursive(remaining, target, partial_rec, ls);
        }
    }

}







