using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;

class Program
{
    const uint MAX_DUNGEONS = 100;

    private static uint n = 6, t = 5, h = 5, d = 15, t1 = 1, t2 = 15;
    private static uint remainingTanks, remainingHealers, remainingDPS;
    private static BlockingCollection<bool>[] dungeonQueues = Array.Empty<BlockingCollection<bool>>();
    private static DungeonState[] dungeonStatus = Array.Empty<DungeonState>();
    private static DungeonStats[] dungeonStats = Array.Empty<DungeonStats>();
    private static object syncLock = new object();
    private static uint totalPartiesCompleted = 0;
    private static uint totalClearTime = 0;
    private static List<Thread> dungeonThreads = new List<Thread>();

    enum DungeonState
    {
        Empty,
        Active
    }

    struct DungeonStats
    {
        public uint Clears;
        public uint ClearTime;
    }

    static void Main()
    {
        if (!File.Exists("config.txt"))
        {
            Console.WriteLine("'config.txt' not found. Please create the file and try again.");
            return;
        }

        LoadInput("config.txt");

        Console.WriteLine("\n--- VALUES USED ---");
        Console.WriteLine($"Dungeons (n): {n}");
        Console.WriteLine($"Tanks (t): {t}");
        Console.WriteLine($"Healers (h): {h}");
        Console.WriteLine($"DPS (d): {d}");
        Console.WriteLine($"Min Clear Time (t1): {t1}");
        Console.WriteLine($"Max Clear Time (t2): {t2}");
        Console.WriteLine("---------------------------\n");

        remainingTanks = t;
        remainingHealers = h;
        remainingDPS = d;

        dungeonQueues = new BlockingCollection<bool>[n];
        dungeonStatus = new DungeonState[n];
        dungeonStats = new DungeonStats[n];

        for (int i = 0; i < n; i++)
        {
            dungeonQueues[i] = new BlockingCollection<bool>(1);
            dungeonStatus[i] = DungeonState.Empty;
            int dungeonId = i;
            Thread thread = new Thread(() => RunDungeon(dungeonId));
            thread.Start();
            dungeonThreads.Add(thread);
        }

        Thread dispatcher = new Thread(DispatchParties);
        dispatcher.Start();
        dispatcher.Join();

        for (int i = 0; i < n; i++)
        {
            dungeonQueues[i].CompleteAdding();
        }

        foreach (var thread in dungeonThreads)
        {
            thread.Join();
        }

        PrintFinalStats();
    }

    static void LoadInput(string path)
    {
        try
        {
            foreach (string line in File.ReadLines(path))
            {
                string clean = line.Split("//")[0].Trim();
                if (string.IsNullOrWhiteSpace(clean)) continue;

                string[] parts = clean.Split('=');
                if (parts.Length != 2) continue;

                string key = parts[0].Trim().ToLower();
                string str = parts[1].Trim();

                if (!uint.TryParse(str, out uint val))
                    continue;

                switch (key)
                {
                    case "n":
                        if (val == 0) Console.WriteLine("Invalid or missing value for dungeon instances. Defaulting to 6.");
                        n = val == 0 ? 6 : val > MAX_DUNGEONS ? MAX_DUNGEONS : val;
                        if (val > MAX_DUNGEONS)
                            Console.WriteLine($"You've set too many dungeons. Capping at {MAX_DUNGEONS}.");
                        break;
                    case "t":
                        if (val == 0) Console.WriteLine("Number of tanks is missing or invalid. Defaulting to 5.");
                        t = val == 0 ? 5 : val;
                        break;
                    case "h":
                        if (val == 0) Console.WriteLine("Number of healers is missing or invalid. Defaulting to 5.");
                        h = val == 0 ? 5 : val;
                        break;
                    case "d":
                        if (val == 0) Console.WriteLine("Number of DPS is missing or invalid. Defaulting to 15.");
                        d = val == 0 ? 15 : val;
                        break;
                    case "t1":
                        if (val == 0) Console.WriteLine("Minimum clear time is missing or invalid. Defaulting to 1 second.");
                        t1 = val == 0 ? 1 : val;
                        break;
                    case "t2":
                        if (val == 0 || val > 15)
                            Console.WriteLine("Maximum dungeon clear time (t2) can't be more than 15. Defaulting to 15 seconds.");
                        t2 = (val == 0 || val > 15) ? 15 : val;
                        break;
                }
            }

            if (t1 > t2)
            {
                Console.WriteLine("Minimum time (t1) is greater than maximum (t2). Swapping them.");
                (t1, t2) = (t2, t1);
            }
        }
        catch
        {
            Console.WriteLine("There was a problem reading the config file. Using default values.");
        }
    }

    static void DispatchParties()
    {
        int nextDungeon = 0;

        while (true)
        {
            bool assigned = false;
            int chosenDungeon = -1;

            lock (syncLock)
            {
                if (remainingTanks >= 1 && remainingHealers >= 1 && remainingDPS >= 3)
                {
                    for (int i = 0; i < n; i++)
                    {
                        int tryDungeon = (nextDungeon + i) % (int)n;

                        if (dungeonStatus[tryDungeon] == DungeonState.Empty)
                        {
                            dungeonStatus[tryDungeon] = DungeonState.Active;

                            remainingTanks--;
                            remainingHealers--;
                            remainingDPS -= 3;

                            chosenDungeon = tryDungeon;
                            nextDungeon = (tryDungeon + 1) % (int)n;
                            assigned = true;
                            break;
                        }
                    }
                }
            }

            if (assigned && chosenDungeon != -1)
            {
                dungeonQueues[chosenDungeon].Add(true); 
            }

            if (!assigned)
            {
                Thread.Sleep(100);

                lock (syncLock)
                {
                    bool anyActive = false;
                    for (int i = 0; i < n; i++)
                    {
                        if (dungeonStatus[i] == DungeonState.Active)
                        {
                            anyActive = true;
                            break;
                        }
                    }

                    if (!(remainingTanks >= 1 && remainingHealers >= 1 && remainingDPS >= 3) && !anyActive)
                        break;
                }
            }
        }
    }


    static void RunDungeon(int id)
    {
        Random rng = new Random(Guid.NewGuid().GetHashCode());

        foreach (var x in dungeonQueues[id].GetConsumingEnumerable())
        {
            lock (syncLock)
            {
                dungeonStatus[id] = DungeonState.Active;
                PrintDungeonStatus();
            }

            int clearTime = rng.Next((int)t1, (int)t2 + 1);
            Thread.Sleep(clearTime * 1000);

            lock (syncLock)
            {
                dungeonStatus[id] = DungeonState.Empty;
                dungeonStats[id].Clears++;
                dungeonStats[id].ClearTime += (uint)clearTime;
                totalPartiesCompleted++;
                totalClearTime += (uint)clearTime;
                PrintDungeonStatus();
            }
        }
    }

    static void PrintDungeonStatus()
    {
        Console.WriteLine("\nDUNGEON STATUS:");
        for (int i = 0; i < n; i++)
        {
            string state = dungeonStatus[i].ToString();
            Console.WriteLine($"Dungeon {i + 1}: {state} | Parties Cleared: {dungeonStats[i].Clears}, Clear Time: {dungeonStats[i].ClearTime}s");
        }
        Console.WriteLine($"Total Parties: {totalPartiesCompleted}, Total Clear Time: {totalClearTime}s\n");
    }

    static void PrintFinalStats()
    {
        Console.WriteLine("\n--- FINAL SUMMARY ---");
        for (int i = 0; i < n; i++)
        {
            Console.WriteLine($"Dungeon {i + 1}:");
            Console.WriteLine($"  Parties Served : {dungeonStats[i].Clears}");
            Console.WriteLine($"  Total Clear Time : {dungeonStats[i].ClearTime} seconds");
        }

        Console.WriteLine($"\nTotal Parties Served: {totalPartiesCompleted}");
        Console.WriteLine($"Total Time Spent Across All Dungeons: {totalClearTime} seconds");

        Console.WriteLine($"\nRemaining Players (not assigned to a dungeon):");
        Console.WriteLine($"  Tanks  : {remainingTanks}");
        Console.WriteLine($"  Healers: {remainingHealers}");
        Console.WriteLine($"  DPS    : {remainingDPS}");
        Console.WriteLine("----------------------\n");
    }
}
