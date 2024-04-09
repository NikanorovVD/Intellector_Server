using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IntellectorServer
{
    public class TimeController
    {
        public int WhiteTime { get; private set; }
        public int BlackTime { get; private set; }

        private int max_time;
        private int added_time;
        private DateTime lust_time;
        private bool turn;
        private bool time_run;
        private bool game_alive;

        public delegate void TimeOut(bool team);
        public event TimeOut TimeOutEvent;

        private Thread TimeOutLooker;
        public TimeController(TimeContol timeContol)
        {
            max_time = timeContol.max_time;
            added_time = timeContol.added_time;
            game_alive = true;
            TimeOutLooker = new Thread(LookForOutOfTime);
            TimeOutLooker.Start();
        }

        public void Start()
        {
            WhiteTime = max_time;
            BlackTime = max_time;
            lust_time = DateTime.Now;
            turn = false;
            time_run = true;
        }        

        public void WhiteMakeMove()
        {
            DateTime recevie_moment = DateTime.Now; 
            TimeSpan elapsed_time = recevie_moment - lust_time;
            lust_time = recevie_moment;
            SubtractWhiteTime((int)elapsed_time.TotalMilliseconds);
            turn = true;
        }
        public void BlackMakeMove()
        {
            DateTime recevie_moment = DateTime.Now;
            TimeSpan elapsed_time = recevie_moment - lust_time;
            lust_time = recevie_moment;
            SubtractBlackTime((int)elapsed_time.TotalMilliseconds);
            turn = false;
        }

        public void Stop()
        {
            game_alive = false;
            TimeOutLooker.Join();
        }

        private void SubtractWhiteTime(int time)
        {
            Console.WriteLine($"потрачено времени белыми: {time}");
            WhiteTime -= time;
            Console.WriteLine($"временя белых: {WhiteTime}");
            if (WhiteTime >= 0) WhiteTime += added_time;
            else
            {
                time_run = false;
                TimeOutEvent?.Invoke(false);
            }
        }

        private void SubtractBlackTime(int time)
        {
            Console.WriteLine($"потрачено времени черными: {time}");
            BlackTime -= time;
            Console.WriteLine($"временя черных: {BlackTime}");
            if (BlackTime >= 0) BlackTime += added_time;
            else
            {
                time_run = false;
                TimeOutEvent?.Invoke(true);
            }
        }

        private void LookForOutOfTime()
        {
            try
            {
                const int look_time = 500;
                while (game_alive)
                {
                    if (time_run)
                    {
                        DateTime now = DateTime.Now;
                        int elapsed_time = (int)(now - lust_time).TotalMilliseconds;
                        if (turn && (elapsed_time >= BlackTime) || !turn && (elapsed_time >= WhiteTime))
                        {
                            TimeOutEvent?.Invoke(turn);
                            time_run = false;
                            return;
                        }
                        Thread.Sleep(look_time);
                    }
                }
            }
            catch (Exception e) { LogWriter.WriteLine(e.ToString()); }
        }
    }
}
