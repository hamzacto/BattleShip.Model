using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BattleShip.API.Model
{
    public class GameState
    {

        public string GameId { get; set; }
        public string CurrentPlayerId { get; set; } // Le joueur actuel
        public string OpponentPlayerId { get; set; } // Joueur opposé (utile pour le multijoueur)
        public List<List<char>> PlayerGrid { get; set; }
        public List<List<char>> OpponentGrid { get; set; }
        //public bool?[][] MaskedPlayerGrid { get; set; }
        //spublic bool?[][] MaskedOppenentGrid { get; set; }
        public List<ShipStatus> PlayerShips { get; set; } // Les bateaux du joueur
        public List<ShipStatus> OpponentShips { get; set; } // Les bateaux de l'adversaire
        public BattleHistory History { get; set; } // Historique des coups
        public bool IsGameOver { get; private set; } // Indique si le jeu est terminé
        public string? WinnerId { get; set; } // Identifiant du gagnant 
        public int GameMode { get; set; }
        public int IaLvl { get; set; }
        public int PVE { get; set; }
       

        private Dictionary<(int X, int Y), Guid> gridToShipMap = new Dictionary<(int X, int Y), Guid>();

        public GameState(string playerId, string opponentPlayerId)
        {
            GameId = Guid.NewGuid().ToString();
            CurrentPlayerId = playerId;
            OpponentPlayerId = opponentPlayerId;

            // Initialize PlayerGrid and OpponentGrid as List<List<char>>
            PlayerGrid = InitializeGrid(); // Initialize the player's grid
            OpponentGrid = InitializeGrid(); // Initialize the opponent's grid

            PlayerShips = InitializeShips(); // Initialize the player's ships
            OpponentShips = InitializeShips(); // Initialize the opponent's ships

            // Place ships on both grids
            PlaceShipsOnGrid(PlayerShips, PlayerGrid);
            PlaceShipsOnGrid(OpponentShips, OpponentGrid);

            History = new BattleHistory
            {
                GameId = GameId, // Assign the GameId
                Moves = new List<BattleMove>()
            }; // Initialize the history
            IsGameOver = false;
            WinnerId = null;
        }

        public GameState() // Parameterless constructor for EF Core
        {
            PlayerGrid = new List<List<char>>();
            OpponentGrid = new List<List<char>>();
            PlayerShips = new List<ShipStatus>();
            OpponentShips = new List<ShipStatus>();
            History = new BattleHistory();
        }

        private List<List<char>> InitializeGrid()
        {
            // Create a 10x10 grid initialized with '\0'
            return Enumerable.Range(0, 10)
                             .Select(_ => Enumerable.Repeat('\0', 10).ToList())
                             .ToList();
        }
        public bool MakeMove(string playerId, int x, int y)
        {
            // Check if the game is over or if it's not the player's turn
            if (IsGameOver || CurrentPlayerId != playerId)
                return false;

            // Identify which grid and ships to target
            var targetGrid = (CurrentPlayerId == playerId) ? OpponentGrid : PlayerGrid;
            var targetShips = (CurrentPlayerId == playerId) ? OpponentShips : PlayerShips;

            // Check if the move is valid (i.e., not hitting the same spot again)
            char targetCell = targetGrid[x][y];
            bool isHit = targetCell != '\0' && targetCell != 'O' && targetCell != 'X'; // A hit occurs if not already hit or missed
            int hitShipId = GetShipIdAt(x, y, targetGrid); // Get the unique ship ID if it's a hit

            // Record the move in the battle history
            var move = new BattleMove
            {
                //MoveNumber = History.Moves.Count + 1,
                PlayerId = playerId,
                X = x,
                Y = y,
                IsHit = isHit,
                Timestamp = DateTime.UtcNow
            };
            History.Moves.Add(move);

            // Update the grid and ship status
            if (isHit)
            {
                move.hitedShipType = GetShipTypeAt(x,y,targetGrid);
                // Mark the grid as hit
                targetGrid[x][y] = 'X';

                // Update the hit ship's status using its ID
                UpdateShipStatus(targetShips, hitShipId);
            }
            else
            {
                targetGrid[x][y] = 'O'; // Mark as a miss
            }

            // Check if the game is over
            CheckGameOver();

            // Switch to the next player's turn
            SwitchCurrentPlayer();

            return isHit;
        }

        // Get the ship ID at the specified grid position
        private int GetShipIdAt(int x, int y, List<List<char>> grid)
        {
            // Assuming that the grid contains the ship ID in its cells
            // Implement your logic to map grid positions to ship IDs
            // For example, if the grid stores ship types or identifiers, retrieve the corresponding ship ID
            char shipChar = grid[x][y];
            // This is a placeholder implementation; adjust according to your grid's structure
            return shipChar == '\0' ? -1 : Convert.ToInt32(shipChar); // Example conversion, use your actual mapping
        }


        public bool UndoLastMove()
        {
            if (History.Moves.Count == 0)
                return false; // No moves to undo

            var lastMove = History.Moves.Last();
            History.Moves.RemoveAt(History.Moves.Count - 1);

            // Determine the target grid and ships based on the player
            var targetGrid = lastMove.PlayerId == CurrentPlayerId ? OpponentGrid : PlayerGrid;
            var targetShips = lastMove.PlayerId == CurrentPlayerId ? OpponentShips : PlayerShips;

            if (lastMove.IsHit)
            {
                // Get the ship type at the specified coordinates
                //char shipType = GetShipTypeAt(lastMove.X, lastMove.Y, targetGrid);

                char shipType = lastMove.hitedShipType.Value;

                // Update ship status for undo
                UpdateShipStatus(targetShips, shipType, undo: true);

                // Restore the original ship type in the grid
                targetGrid[lastMove.X][lastMove.Y] = shipType; // Change from targetGrid[lastMove.X, lastMove.Y] to targetGrid[lastMove.X][lastMove.Y]
            }
            else
            {
                // Restore the cell to empty
                targetGrid[lastMove.X][lastMove.Y] = '\0'; // Change from targetGrid[lastMove.X, lastMove.Y] to targetGrid[lastMove.X][lastMove.Y]
            }

            // Reset the game state
            IsGameOver = false;
            WinnerId = null;
            SwitchCurrentPlayer();

            return true;
        }
        private char GetShipTypeAt(int x, int y, List<List<char>> grid)
        {
            return grid[x][y]; // Access the grid correctly using List<List<char>>
        }

        private void UpdateShipStatus(List<ShipStatus> ships, int shipId, bool undo = false)
        {
            var ship = ships.FirstOrDefault(s => s.ShipType == shipId);
            if (ship != null)
            {
                ship.HitCount += undo ? -1 : 1; // Increment or decrement hit count based on undo flag
            }
        }

        private void CheckGameOver()
        {
            if (PlayerShips.All(s => s.IsSunk))
            {
                IsGameOver = true;
                WinnerId = OpponentPlayerId;
            }
            else if (OpponentShips.All(s => s.IsSunk))
            {
                IsGameOver = true;
                WinnerId = CurrentPlayerId;
            }
        }

        private void SwitchCurrentPlayer()
        {
            CurrentPlayerId = CurrentPlayerId == OpponentPlayerId ? OpponentPlayerId : CurrentPlayerId;
        }

        private char GetShipTypeAt(char[,] grid, int x, int y)
        {
            return grid[x, y];
        }

        private List<ShipStatus> InitializeShips()
        {
            return new List<ShipStatus>
        {
            new ShipStatus { ShipType = 'A', ShipName = "Porte-avions", Size = 5 },
            new ShipStatus { ShipType = 'B', ShipName = "Cuirassé", Size = 4 },
            new ShipStatus { ShipType = 'C', ShipName = "Croiseur", Size = 3 },
            new ShipStatus { ShipType = 'S', ShipName = "Sous-marin", Size = 3 },
            new ShipStatus { ShipType = 'D', ShipName = "Destroyer", Size = 2 }
        };
        }


        private void PlaceShipsOnGrid(List<ShipStatus> ships, List<List<char>> grid)
        {
            Random random = new Random();

            foreach (var ship in ships)
            {
                bool placed = false;

                while (!placed)
                {
                    bool isHorizontal = random.Next(0, 2) == 0;
                    int startX = random.Next(0, 10);
                    int startY = random.Next(0, 10);

                    if (CanPlaceShip(grid, ship, startX, startY, isHorizontal))
                    {
                        for (int i = 0; i < ship.Size; i++)
                        {
                            int x = startX + (isHorizontal ? i : 0);
                            int y = startY + (isHorizontal ? 0 : i);

                            grid[x][y] = ship.ShipType; // Mark the ship on the grid with its ShipType
                            gridToShipMap[(x, y)] = ship.ShipId; // Map position to ShipId
                        }
                        placed = true;
                    }
                }
            }
        }

        private bool CanPlaceShip(List<List<char>> grid, ShipStatus ship, int startX, int startY, bool isHorizontal)
        {
            if (isHorizontal)
            {
                if (startX + ship.Size > 10)
                    return false;
            }
            else
            {
                if (startY + ship.Size > 10)
                    return false;
            }

            for (int i = 0; i < ship.Size; i++)
            {
                int x = startX + (isHorizontal ? i : 0);
                int y = startY + (isHorizontal ? 0 : i);

                if (grid[x][y] != '\0') // '\0' represents an empty space
                    return false;
            }

            return true;
        }

        private void UpdateShipStatus(List<ShipStatus> ships, char shipType)
        {
            var ship = ships.FirstOrDefault(s => s.ShipType == shipType);
            if (ship != null)
            {
                ship.HitCount++;

            }
        }

    }

    public class ShipStatus
    {
        public Guid ShipId { get; set; } = Guid.NewGuid(); // Unique ID for each ship
        public char ShipType { get; set; } // E.g., 'A' for Aircraft Carrier, 'D' for Destroyer
        public string ShipName { get; set; }
        public int Size { get; set; }
        public int HitCount { get; set; }
        public bool IsSunk => HitCount >= Size; // Check if the ship is sunk

        // Foreign key to GameState
        /*public string GameStateId { get; set; }
        public GameState GameState { get; set; } // Navigation property*/
    }   


}