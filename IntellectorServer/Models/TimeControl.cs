namespace IntellectorServer.Models
{
    public class TimeContol
    {
        public int max_time;
        public int added_time;

        public int MaxMinutes
        {
            get { return max_time / 60000; }
            set { max_time = value * 60000; }
        }
        public int AddedSeconds
        {
            get { return added_time / 1000; }
            set { added_time = value * 1000; }
        }
        public TimeContol(int minutes, int add_seconds)
        {
            MaxMinutes = minutes;
            AddedSeconds = add_seconds;
        }
        public override string ToString()
        {
            if (max_time == 0) return "Unlimit";
            return $"{MaxMinutes} + {AddedSeconds}";
        }
    }
}
