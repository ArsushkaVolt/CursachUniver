using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Forms;

namespace TetrisOOP
{
    // ====================== ИНТЕРФЕЙСЫ ======================
    public interface IGameObject { void Draw(Graphics g); }                 // Всё, что рисуется на экране
    public interface IMovable { void MoveLeft(); void MoveRight(); void MoveDown(); void Rotate(); } // Движение и вращение

    // ====================== ОСНОВНАЯ ФИГУРА ======================
    public abstract class Tetromino : IGameObject, IMovable
    {
        public Point[][] shapes = null!;        // Все возможные повороты фигуры
        public Point position;                  // Текущая позиция на поле (x, y)
        public int currentRotation;             // Текущий поворот (0..3)
        public Color color;                     // Цвет фигуры
        protected GameField game = null!;       // Ссылка на игровое поле

        protected Tetromino(GameField field)
        {
            game = field;
            position = new Point(4, -1);        // Все фигуры появляются сверху по центру
        }

        public abstract void Reset();           // У каждой фигуры своя форма и цвет

        // Рисуем текущую фигуру
        public virtual void Draw(Graphics g)
        {
            foreach (Point p in shapes[currentRotation])
            {
                int x = (position.X + p.X) * 30 + 400;  // Смещение поля вправо
                int y = (position.Y + p.Y) * 30 + 50;   // Смещение поля вниз
                g.FillRectangle(new SolidBrush(color), x, y, 29, 29);
                g.DrawRectangle(Pens.Black, x, y, 29, 29); // Обводка для красоты
            }
        }

        // Проверка: влезает ли фигура в поле и не пересекается ли с другими блоками
        public bool IsValid()
        {
            foreach (Point p in shapes[currentRotation])
            {
                int x = position.X + p.X;
                int y = position.Y + p.Y;
                if (x < 0 || x >= 10 || y >= 20) return false;     // Выход за границы
                if (y < 0) continue;                              // Над полем — нормально
                if (game.board[y, x] == 1) return false;           // Столкновение с застывшим блоком
            }
            return true;
        }

        public void MoveLeft() { position.X--; if (!IsValid()) position.X++; }
        public void MoveRight() { position.X++; if (!IsValid()) position.X--; }
        public void MoveDown() { position.Y++; if (!IsValid()) { position.Y--; game.LockPiece(); } }
        public virtual void Rotate()
        {
            int old = currentRotation;
            currentRotation = (currentRotation + 1) % shapes.Length;
            if (!IsValid()) currentRotation = old; // Откатываем поворот, если нельзя
        }
    }

    // ====================== КОНКРЕТНЫЕ ФИГУРЫ ======================
    public class I_Tetromino : Tetromino { public I_Tetromino(GameField f) : base(f) => Reset(); public override void Reset() { color = Color.Cyan; shapes = new[] { new[] { new Point(0, 1), new Point(1, 1), new Point(2, 1), new Point(3, 1) }, new[] { new Point(2, 0), new Point(2, 1), new Point(2, 2), new Point(2, 3) } }; } }
    public class O_Tetromino : Tetromino { public O_Tetromino(GameField f) : base(f) => Reset(); public override void Reset() { color = Color.Yellow; shapes = new[] { new[] { new Point(1, 0), new Point(2, 0), new Point(1, 1), new Point(2, 1) } }; } public override void Rotate() { } } // Квадрат не крутится
    public class T_Tetromino : Tetromino { public T_Tetromino(GameField f) : base(f) => Reset(); 
        public override void Reset() 
        { 
            color = Color.Magenta; 
            shapes = new[] 
            { 
                new[] { new Point(1, 0), new Point(0, 1), new Point(1, 1), new Point(2, 1) }, 
                new[] { new Point(1, 0), new Point(1, 1), new Point(2, 1), new Point(1, 2) }, 
                new[] { new Point(1, 1), new Point(0, 2), new Point(1, 2), new Point(2, 2) }, 
                new[] { new Point(1, 0), new Point(0, 1), new Point(1, 1), new Point(1, 2) } 
            }; 
        } 
    }

    // ====================== ТАБЛО ======================
    public class ScoreDisplay : IGameObject
    {
        public int Score, Lines, Level = 1;

        public void Draw(Graphics g)
        {
            string text = $"   СЧЁТ: {Score}\n" +
                          $"   ЛИНИИ: {Lines}\n" +
                          $"   УРОВЕНЬ: {Level}\n\n" +
                          "   УПРАВЛЕНИЕ:\n" +
                          "   ↑ — вращать\n" +
                          "   ← → — движение\n" +
                          "   ↓ — ускорить\n" +
                          "   P — пауза";

            g.DrawString(text, new Font("Consolas", 14, FontStyle.Bold), Brushes.White, 700, 100);
        }
    }

    // ====================== ИГРОВОЕ ПОЛЕ ======================
    public class GameField : IGameObject
    {
        public readonly int[,] board = new int[20, 10];  // 20 строк × 10 столбцов
        public Tetromino? Current;                       // Текущая падающая фигура
        public readonly ScoreDisplay ScoreBoard = new();
        public bool GameOver { get; private set; }

        private readonly Type[] pieces = { typeof(I_Tetromino), typeof(O_Tetromino), typeof(T_Tetromino) };
        private readonly Random rnd = new();

        // Новая игра — очищаем поле, сбрасываем счёт
        public void NewGame()
        {
            Array.Clear(board, 0, board.Length);
            ScoreBoard.Score = ScoreBoard.Lines = 0;
            ScoreBoard.Level = 1;
            GameOver = false;
            Spawn();
        }

        // Появление новой фигуры
        public void Spawn()
        {
            if (GameOver) return;
            Current = (Tetromino)Activator.CreateInstance(pieces[rnd.Next(pieces.Length)], this);
            if (!Current.IsValid())
            {
                GameOver = true;
                Current = null;
            }
        }

        // Фиксация фигуры на поле
        public void LockPiece()
        {
            if (Current == null || GameOver) return;
            foreach (Point p in Current.shapes[Current.currentRotation])
            {
                int x = Current.position.X + p.X;
                int y = Current.position.Y + p.Y;
                if (y >= 0) board[y, x] = 1;
            }
            RemoveLines();
            Spawn();
        }

        // Удаление заполненных линий
        private void RemoveLines()
        {
            int removed = 0;
            for (int y = 19; y >= 0; y--)
            {
                if (Enumerable.Range(0, 10).All(x => board[y, x] == 1))
                {
                    for (int yy = y; yy > 0; yy--)
                        Array.Copy(board, (yy - 1) * 10, board, yy * 10, 10);
                    Array.Clear(board, 0, 10);
                    removed++;
                    y++; // Повторно проверяем эту строку
                }
            }
            if (removed > 0)
            {
                ScoreBoard.Lines += removed;
                ScoreBoard.Score += removed * 100 * ScoreBoard.Level;
                ScoreBoard.Level = 1 + ScoreBoard.Lines / 10;
            }
        }

        // Отрисовка всего поля
        public void Draw(Graphics g)
        {
            // Фон игрового поля
            g.FillRectangle(Brushes.DarkSlateGray, 399, 49, 302, 602);
            g.DrawRectangle(Pens.Cyan, 399, 49, 302, 602);

            // Застывшие блоки
            for (int y = 0; y < 20; y++)
                for (int x = 0; x < 10; x++)
                    if (board[y, x] == 1)
                        g.FillRectangle(Brushes.SlateGray, x * 30 + 400, y * 30 + 50, 29, 29);

            Current?.Draw(g);
            ScoreBoard.Draw(g);

            // Экран Game Over
            if (GameOver)
            {
                g.FillRectangle(new SolidBrush(Color.FromArgb(220, 0, 0, 0)), 430, 225, 265, 160);
                g.DrawString("GAME OVER", new Font("Consolas", 32, FontStyle.Bold), Brushes.Red, 450, 240);
                g.DrawString($"Счёт: {ScoreBoard.Score}", new Font("Consolas", 20), Brushes.White, 480, 310);
            }
        }
    }

    // ====================== ГЛАВНАЯ ФОРМА ======================
    public partial class TetrisForm : Form
    {
        private readonly GameField game = new GameField();
        private readonly System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer(); 

        public TetrisForm()
        {
            InitializeComponent();

            // Настройки окна
            this.Text = "Тетрис — ООП Курсач";
            this.Size = new Size(1200, 800);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(15, 15, 35);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.KeyPreview = true;      // Чтобы клавиши работали
            this.DoubleBuffered = true;  // Без мерцания

            // Левая панель — информация и кнопки
            var leftPanel = new Panel
            {
                Width = 380,
                Dock = DockStyle.Left,
                BackColor = Color.FromArgb(20, 20, 40)
            };

            // Кнопки управления
            var btnNew = CreateButton("НОВАЯ ИГРА", Color.FromArgb(0, 120, 255));
            var btnPause = CreateButton("ПАУЗА", Color.FromArgb(255, 140, 0));
            var btnSave = CreateButton("СОХРАНИТЬ РЕКОРД", Color.FromArgb(0, 180, 0));

            // Обработчики кнопок
            btnNew.Click += (s, e) => { game.NewGame(); Invalidate(); };
            btnPause.Click += (s, e) =>
            {
                timer.Enabled = !timer.Enabled;
                btnPause.Text = timer.Enabled ? "ПАУЗА" : "ПРОДОЛЖИТЬ";
                Invalidate();
            };
            btnSave.Click += (s, e) =>
            {
                var record = new { Score = game.ScoreBoard.Score, Date = DateTime.Now.ToString("dd.MM.yyyy HH:mm") };
                File.WriteAllText("record.json", JsonSerializer.Serialize(record, new JsonSerializerOptions { WriteIndented = true }));
                MessageBox.Show($"Рекорд {record.Score} сохранён!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            // Расположение кнопок под табло
            btnNew.Location = new Point(30, 300);
            btnPause.Location = new Point(30, 370);
            btnSave.Location = new Point(30, 440);

            leftPanel.Controls.Add(btnNew);
            leftPanel.Controls.Add(btnPause);
            leftPanel.Controls.Add(btnSave);
            Controls.Add(leftPanel);

            // Запуск игры
            game.NewGame();

            // Таймер — падение фигуры каждые 500 мс
            timer.Interval = 500;
            timer.Tick += (s, e) =>
            {
                if (!game.GameOver)
                    game.Current?.MoveDown();
                Invalidate(); // Перерисовка
            };
            timer.Start();
        }

        // Создание красивой кнопки
        private Button CreateButton(string text, Color bg)
        {
            return new Button
            {
                Text = text,
                Width = 320,
                Height = 60,
                BackColor = bg,
                ForeColor = Color.White,
                Font = new Font("Consolas", 14, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Location = new Point(30, 0)
            };
        }

        // Перерисовка всей формы
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            game.Draw(e.Graphics);
        }

        // Обработка клавиш (стрелки + P)
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (game.GameOver) return base.ProcessCmdKey(ref msg, keyData);

            switch (keyData)
            {
                case Keys.Left: game.Current?.MoveLeft(); Invalidate(); return true;
                case Keys.Right: game.Current?.MoveRight(); Invalidate(); return true;
                case Keys.Down: game.Current?.MoveDown(); Invalidate(); return true;
                case Keys.Up: game.Current?.Rotate(); Invalidate(); return true;
                case Keys.P: timer.Enabled = !timer.Enabled; Invalidate(); return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}