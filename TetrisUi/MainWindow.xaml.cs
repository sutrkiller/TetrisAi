using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Tetris;
using Tetris.Models;
using Grid = Tetris.Models.Grid;

namespace TetrisUi
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private double[] _fact =
            "-0.20246807;-0.37864260;-0.56586424;-0.01309536;0.21249134;-0.33714135;-0.50522643;-0.08181478;-0.27294390"
            //"-0.34939090;-0.28973177;-0.36981444;-0.41017038;0.02703165;-0.46784607;-0.23669553;-0.21747398;-0.40749071"
            .Split(';').Select(Convert.ToDouble).ToArray();
        //private int[][] _grid;
        private Grid _grid;
        private Cube Next
        {
            get { return _next; }
            set
            {
                if (_next != null && value.GetType() == _next.GetType()) return;
                _next = value;
                DrawNext();
            }
        }
        private Cube _next;

        private Cube Current
        {
            get { return _current; }
            set
            {
                _current = value;
                Rotation = 0;
                Row = -1;
                Column = (_grid?.Width - value?.Rotations[0]?[0]?.Length)/2 ?? 0;
                if (AiRunning)
                {
                    UpdaterAi();
                }
                else
                {
                    Updater();
                }
            }
        }
        private Cube _current;

        private int Rotation { get; set; }
        private int Column { get; set; }
        private int Row { get; set; }


        public bool Ended
        {
            get { return _ended; }
            set
            {
                _ended = value; 
                OnPropertyChanged();
            }
        }

        public int Speed { get; set; } = 1;

        public int ClearedLines
        {
            get { return _clearedLines; }
            private set
            {
                _clearedLines = value;
                OnPropertyChanged();
            }
        }

        public int PlayedMoves
        {
            get { return _playedMoves; }
            private set
            {
                _playedMoves = value;
                OnPropertyChanged();
            }
        }

        public string Ratio => ClearedLines == 0 ? $"{new decimal(PlayedMoves):0.00}" : $"{new decimal(PlayedMoves/(double)ClearedLines):0.00}";

        public bool AiRunning
        {
            get { return _aiRunning; }
            set
            {
                _aiRunning = value;
                OnPropertyChanged();
            }
        }

        private readonly List<Rectangle> _currentRectangles = new List<Rectangle>();
        private int _clearedLines;
        private int _playedMoves;
        private bool _aiRunning = true;
        private bool _ended;
        //private Ai _ai;
        private readonly AiFinal _ai = new AiFinal();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void DrawNext()
        {
            CanvasNext.Children.Clear();
            if (_next == null) return;
            var next = _next.Rotations[0];
            var left = (CanvasNext.ActualWidth-(next[0].Length * 24 + (next[0].Length-1))) / 2;
            var top = (CanvasNext.ActualHeight-(next.Length * 24 + (next.Length-1))) / 2;
            for (int i = 0; i < next.Length; i++)
            {
                for (int j = 0; j < next[0].Length; j++)
                {
                    if (next[i][j] == 0) continue;
                    Rectangle rect = new Rectangle {Width = 24, Height = 24, Fill = Brushes.BlueViolet};
                    CanvasNext.Children.Add(rect);
                    Canvas.SetTop(rect, top + i*24 + i);
                    Canvas.SetLeft(rect, left + j*24 + j);
                }
            }
        }

        private Task _currentTask;

        private void UpdaterAi()
        {
            _currentTask = Task.Run(() =>
            {
                if (_ai == null) return;
                var next = _ai.DiscoverNextMove(_grid, new List<Cube>() {Current, Next});
                if (next == null)
                {
                    Dispatcher.Invoke(() =>
                    {
                        Ended = true;
                    });
                }
                else
                {
                    Dispatcher.Invoke(() =>
                    {
                        Column = next.Column;
                        Rotation = next.Rotation;
                    });
                    while (!Ended && Row < next.Row)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            Row++;
                            DrawCurrent();
                        });
                        Thread.Sleep((101-Speed)*1);
                    }
                }
                if (Row < 0)
                {
                    Dispatcher.Invoke(() =>
                    {
                        Ended = true;
                    });
                }
                else
                {
                    Dispatcher.Invoke(() =>
                    {
                        FinalizeRound();
                    });
                }
            }).ContinueWith(task =>
            {
                if (Ended) return;
                Dispatcher.Invoke(NextRound);
            });
        }

        private void Updater()
        {
            _currentTask = Task.Run(() =>
            {
                while (!Ended && _grid.IsPossible(Current.Rotations[Rotation],Row+1,Column))
                {
                    Dispatcher.Invoke(() =>
                    {
                        Row++;
                        DrawCurrent();
                    });
                    Thread.Sleep((101 - Speed) * 5);
                }
                if (Row < 0)
                {
                    Dispatcher.Invoke(() =>
                    {
                        Ended = true;
                    });
                }
                else
                {
                    Dispatcher.Invoke(() =>
                    {
                        FinalizeRound();
                    });
                }
            }).ContinueWith(task =>
            {
                if (Ended) return;
                Dispatcher.Invoke(NextRound);
            });
        }

        private void RedrawGrid()
        {
            CanvasMain.Children.Clear();
            if (_grid == null) return;
            for (int i = 0; i < _grid.Height; i++)
            {
                for (int j = 0; j < _grid.Width; j++)
                {
                    if (_grid.Cells[i][j] == 0) continue;
                    Rectangle rect = new Rectangle {Width = 24,Height = 24, Fill = Brushes.BlueViolet};
                    CanvasMain.Children.Add(rect);
                    Canvas.SetTop(rect,1+ i*24 + i);
                    Canvas.SetLeft(rect,1+ j*24 + j);
                }
            }
        }

       

        private void DrawCurrent()
        {
            foreach (var rect in _currentRectangles)
            {
                CanvasMain.Children.Remove(rect);
            }
            _currentRectangles.Clear();
            if (_current == null) return;
            var cur = _current.Rotations[Rotation];
            for (int i = 0; i < cur.Length; i++)
            {
                if (i+Row < 0) continue;
                for (int j = 0; j < cur[0].Length; j++)
                {
                    if (cur[i][j] == 0) continue;
                    Rectangle rect = new Rectangle { Width = 24, Height = 24, Fill = Brushes.BlueViolet };
                    _currentRectangles.Add(rect);
                    CanvasMain.Children.Add(rect);
                    Canvas.SetTop(rect, 1 + i * 25 + Row *25);
                    Canvas.SetLeft(rect, 1 + j * 25 + Column*25);
                }
            }

        }

        private void NextRound()
        {
            //++PlayedMoves;
            var next = Next;
            Next = GenerateInput.GenerateNext(1)[0];
            Current = next;
            //Next = GenerateInput.GenerateNext(1)[0];
        }

        private void FinalizeRound()
        {
            if (Ended) return;
            _grid.AddToPosition(Current.Rotations[Rotation], Row, Column);
            ClearedLines += _grid.DeleteRows().Count;
            PlayedMoves++;

            RedrawGrid();
        }

        private void NewGame()
        {
            ClearedLines = 0;
            PlayedMoves = 0;
            Ended = false;
            _grid = new Grid(20, 10);
            var cubes = GenerateInput.GenerateNext(2);
            Current = cubes[0];
            Next = cubes[1];
            RedrawGrid();
        }

        private void WindowMain_Loaded(object sender, RoutedEventArgs e)
        {
            //_grid = new Grid(20,10);
            //_grid = new int[20][];
            //for (int i = 0; i < _grid.Height; i++)
            //{
            //    _grid.Cells[i] = new int[10];
            //    for (int j = 0; j < _grid.Width; j++)
            //    {
            //       if(i==19 && j!=5) _grid.Cells[i][j] = 1;
            //    }
            //}
            //RedrawGrid();
            NewGame();
           

           
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            if (propertyName == nameof(PlayedMoves)) PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Ratio)));
        }

        private void Rotate()
        {
            var rots = _grid.GetPossibleRotations(Current, Row, Column);
            if (!rots.Any()) return;
            do
            {
                Rotation = (Rotation + 1)%Current.Rotations.Count;
            } while (!rots.Contains(Rotation));
        }

        private void WindowMain_KeyDown(object sender, KeyEventArgs e)
        {
            if (AiRunning) return;
            switch (e.Key)
            {
                case Key.Up:
                    Rotate();
                    break;
                case Key.Left:
                    Move(Column - 1);
                    break;
                case Key.Right:
                    Move(Column + 1);
                    break;
                case Key.Down:
                    MoveRow(Row +1);
                    break;
            }
            DrawCurrent();
        }

        private void MoveRow(int newRow)
        {
            if(newRow >= _grid.Height) return;
            if(!_grid.IsPossible(Current.Rotations[Rotation],newRow,Column)) return;
            Row = newRow;
            DrawCurrent();
        }

        private void Move(int newColumn)
        {
            if (newColumn < 0 || newColumn >= _grid.Width) return;
            if (!_grid.IsPossible(Current.Rotations[Rotation], Row, newColumn)) return;
            Column = newColumn;
            DrawCurrent();

        }

        private async void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            ResetButton.IsEnabled = false;
            Ended = true;
            if (_currentTask != null) await _currentTask;
            NewGame();
            ResetButton.IsEnabled = true;
        }

        private void ButtonStartAi_Click(object sender, RoutedEventArgs e)
        {
            AiRunning = !AiRunning;

            this.Focusable = !AiRunning;
            Focus();
        }
    }
}
