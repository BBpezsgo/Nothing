using System.Collections.Generic;

using UnityEngine;

public class TeamManager : SingleInstance<TeamManager>
{
    [System.Serializable]
    public class Team
    {
        [SerializeField] internal string ID;
        [SerializeField] internal Color Color;
        [SerializeField, ReadOnly] internal int Hash;
    }

    [System.Serializable]
    internal class FuckYou
    {
        [SerializeField] internal string Team1;
        [SerializeField, ReadOnly] internal int Team1Hash;
        [SerializeField] internal string Team2;
        [SerializeField, ReadOnly] internal int Team2Hash;

        [SerializeField] internal float FuckYouValue;
    }

    List<string> TeamIDs = new();

    [SerializeField] List<Team> teams = new();
    [SerializeField] List<FuckYou> fuckYous = new();

    protected override void Awake()
    {
        base.Awake();

        for (int i = 0; i < teams.Count; i++)
        {
            string team = teams[i].ID;
            if (TeamIDs.Contains(team)) continue;
            teams[i].Hash = TeamIDs.Count;
            TeamIDs.Add(team);
        }

        for (int i = 0; i < fuckYous.Count; i++)
        {
            string team1 = fuckYous[i].Team1;
            string team2 = fuckYous[i].Team2;

            fuckYous[i].Team1Hash = TeamIDs.IndexOf(team1);
            fuckYous[i].Team2Hash = TeamIDs.IndexOf(team2);

            if (fuckYous[i].Team1Hash == -1)
            { Debug.LogWarning($"[{nameof(TeamManager)}]: Team \"{team1}\" not found"); }

            if (fuckYous[i].Team2Hash == -1)
            { Debug.LogWarning($"[{nameof(TeamManager)}]: Team \"{team2}\" not found"); }
        }
    }

    internal Team GetOrCreateTeam(string id)
    {
        for (int i = 0; i < teams.Count; i++)
        { if (teams[i].ID == id) return teams[i]; }

        if (TeamIDs.Contains(id))
        {
            Debug.LogError($"[{nameof(TeamManager)}]: Team \"{id}\" not found but hash exists");
        }

        Team newTeam = new()
        {
            Color = Color.white,
            ID = id,
            Hash = TeamIDs.Count,
        };
        TeamIDs.Add(id);
        teams.Add(newTeam);
        return newTeam;
    }

    internal Team GetTeam(string id)
    {
        for (int i = teams.Count - 1; i >= 0; i--)
        { if (teams[i].ID == id) return teams[i]; }
        return null;
    }

    internal Team GetTeam(int hash)
    {
        for (int i = teams.Count - 1; i >= 0; i--)
        { if (teams[i].Hash == hash) return teams[i]; }
        return null;
    }

    internal float GetFuckYou(BaseObject objectA, BaseObject objectB)
    {
        if (objectA == null || objectB == null) return 0f;
        return GetFuckYou(objectA.Team, objectB.Team);
    }
    internal float GetFuckYou(string teamA, string teamB)
    {
        if (string.IsNullOrEmpty(teamA) || string.IsNullOrEmpty(teamB)) return 0f;

        for (int i = fuckYous.Count - 1; i >= 0; i--)
        {
            if ((fuckYous[i].Team1 == teamA && fuckYous[i].Team2 == teamB) ||
                fuckYous[i].Team2 == teamA && fuckYous[i].Team1 == teamB)
            { return fuckYous[i].FuckYouValue; }
        }
        return 0f;
    }
    internal float GetFuckYou(int teamA, int teamB)
    {
        if (teamA == -1 || teamB == -1) return 0f;

        for (int i = fuckYous.Count - 1; i >= 0; i--)
        {
            if ((fuckYous[i].Team1Hash == teamA && fuckYous[i].Team2Hash == teamB) ||
                fuckYous[i].Team2Hash == teamA && fuckYous[i].Team1Hash == teamB)
            { return fuckYous[i].FuckYouValue; }
        }
        return 0f;
    }

    internal void SetFuckYou(string teamA, string teamB, float value)
    {
        if (string.IsNullOrEmpty(teamA) || string.IsNullOrEmpty(teamB)) return;

        for (int i = 0; i < fuckYous.Count; i++)
        {
            if ((fuckYous[i].Team1 == teamA && fuckYous[i].Team2 == teamB) ||
                fuckYous[i].Team2 == teamA && fuckYous[i].Team1 == teamB)
            {
                fuckYous[i].FuckYouValue = value;
            }
        }
    }
}
