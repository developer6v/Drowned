using System;
using System.Collections.Generic;
using Drowned.Resources;

namespace Drowned.Save
{
    /// <summary>
    /// Foto da partida para gravar em JSON com <c>JsonUtility</c> (SCRUM-176). Só dados, sem lógica.
    /// Campos públicos porque é o que o JsonUtility serializa.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        public const int CurrentVersion = 1;

        [Serializable]
        public class ClockSave
        {
            public int day = 1;
            public int tickOfDay;
            public double simulatedSeconds;
            public double playedSeconds;
        }

        [Serializable]
        public class StructureSave
        {
            public string id;
            public int x;
            public int y;
            public int level = 1;
        }

        [Serializable]
        public class ResearchSave
        {
            public List<string> researched = new List<string>();
            public string current;
            public float currentPoints;
            public float storedPoints;
        }

        [Serializable]
        public class MonumentSave
        {
            public int stage;
            public ResourceBundle delivered;
            public bool active;
        }

        [Serializable]
        public class StatsSave
        {
            public int peakPopulation;
            public int structuresBuilt;
            public int upgradesDone;
            public int technologiesResearched;
            public int monumentStagesCompleted;
        }

        public int version = CurrentVersion;
        /// <summary>Data/hora da gravação em ISO 8601, para o menu "Continuar".</summary>
        public string savedAt;

        public ClockSave clock = new ClockSave();
        public ResourceBundle stock;
        public int population;
        public List<StructureSave> structures = new List<StructureSave>();
        public List<string> specialists = new List<string>();
        public ResearchSave research = new ResearchSave();
        public MonumentSave monument = new MonumentSave();
        public int collapseDays;
        public StatsSave stats = new StatsSave();
    }
}
