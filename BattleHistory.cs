using System.ComponentModel.DataAnnotations;

namespace BattleShip.API.Model
{
        public class BattleHistory
        {
        /* public int MoveNumber { get; set; }
         public string PlayerId { get; set; }
         public int X { get; set; }
         public int Y { get; set; }
         public bool IsHit { get; set; }
         public DateTime Timestamp { get; set; }*/

        // Primary key
        public int Id { get; set; }  // Primary key

        public string GameId { get; set; }  // Foreign key to GameState
        
        public List<BattleMove> Moves { get; set; } = new List<BattleMove>();
    }
}
