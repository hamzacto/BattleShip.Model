namespace BattleShip.API.Model
{
    public class LeaderboardEntry
    {
        public string PlayerId { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public int TotalGames { get; set; }
        public double WinRate => TotalGames > 0 ? (double)Wins / TotalGames : 0;
    }


}