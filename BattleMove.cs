using System.ComponentModel.DataAnnotations;

namespace BattleShip.API.Model
{
    public class BattleMove
    {
        // Composite key: MoveNumber and GameId (or BattleHistoryId)
        // Primary key
        public int MoveNumber { get; set; }
        public string PlayerId { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public bool IsHit { get; set; }
        public char? hitedShipType { get; set; }
        public DateTime Timestamp { get; set; }
        // Foreign key to BattleHistory
        public int BattleHistoryId { get; set; }  // Foreign key to BattleHistory
        // Navigation property
        public BattleHistory BattleHistory { get; set; }  // Navigation property
    }
}
