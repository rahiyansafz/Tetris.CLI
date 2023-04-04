using System.Diagnostics;

namespace Tetris.CLI;
internal class App
{
    // Map / BG 
    const int _mapSizeX = 10;
    const int _mapSizeY = 20;
    static readonly char[,] _bg = new char[_mapSizeY, _mapSizeX];

    static int _score = 0;

    // Hold variables
    const int _holdSizeX = 6;
    const int _holdSizeY = _mapSizeY;
    static int _holdIndex = -1;
    static char _holdChar;

    const int _upNextSize = 6;

    static ConsoleKeyInfo _input;

    // Current info
    static int _currentX = 0;
    static int _currentY = 0;
    static char _currentChar = 'O';
    static int _currentRot = 0;

    // Block and Bogs        
    static int[]? _bag;
    static int[]? _nextBag;

    static int _bagIndex;
    static int _currentIndex;

    // misc
    static readonly int _maxTime = 20;
    static int _timer = 0;
    static int _amount = 0;

    #region Assets

    /* Possible modification
    readonly static ConsoleColor[] colours = 
    {
        ConsoleColor.Red,
        ConsoleColor.Blue,
        ConsoleColor.Green,
        ConsoleColor.Magenta,
        ConsoleColor.Yellow,
        ConsoleColor.White,
        ConsoleColor.Cyan
    };
    */

    readonly static string _characters = "OILJSZT";
    readonly static int[,,,] _positions =
    {
        {
        {{0,0},{1,0},{0,1},{1,1}},
        {{0,0},{1,0},{0,1},{1,1}},
        {{0,0},{1,0},{0,1},{1,1}},
        {{0,0},{1,0},{0,1},{1,1}}
        },

        {
        {{2,0},{2,1},{2,2},{2,3}},
        {{0,2},{1,2},{2,2},{3,2}},
        {{1,0},{1,1},{1,2},{1,3}},
        {{0,1},{1,1},{2,1},{3,1}},
        },
        {
        {{1,0},{1,1},{1,2},{2,2}},
        {{1,2},{1,1},{2,1},{3,1}},
        {{1,1},{2,1},{2,2},{2,3}},
        {{2,1},{2,2},{1,2},{0,2}}
        },

        {
        {{2,0},{2,1},{2,2},{1,2}},
        {{1,1},{1,2},{2,2},{3,2}},
        {{2,1},{1,1},{1,2},{1,3}},
        {{0,1},{1,1},{2,1},{2,2}}
        },

        {
        {{2,1},{1,1},{1,2},{0,2}},
        {{1,0},{1,1},{2,1},{2,2}},
        {{2,1},{1,1},{1,2},{0,2}},
        {{1,0},{1,1},{2,1},{2,2}}
        },
        {
        {{0,1},{1,1},{1,2},{2,2}},
        {{1,0},{1,1},{0,1},{0,2}},
        {{0,1},{1,1},{1,2},{2,2}},
        {{1,0},{1,1},{0,1},{0,2}}
        },

        {
        {{0,1},{1,1},{1,0},{2,1}},
        {{1,0},{1,1},{2,1},{1,2}},
        {{0,1},{1,1},{1,2},{2,1}},
        {{1,0},{1,1},{0,1},{1,2}}
        }
        };
    #endregion

    internal static void Run()
    {
        // Make the console cursor invisible
        Console.CursorVisible = false;

        // Title
        Console.Title = "Tetris | By: Kat9_123";

        // Start the inputthread to get live inputs
        Thread inputThread = new(Input);
        inputThread.Start();

        // Generate bag / current block
        _bag = GenerateBag();
        _nextBag = GenerateBag();
        NewBlock();

        // Generate an empty bg
        for (int y = 0; y < _mapSizeY; y++)
            for (int x = 0; x < _mapSizeX; x++)
                _bg[y, x] = '-';

        while (true)
        {
            // Force block down
            if (_timer >= _maxTime)
            {
                // If it doesn't collide, just move it down. If it does call BlockDownCollision
                if (!Collision(_currentIndex, _bg, _currentX, _currentY + 1, _currentRot)) _currentY++;
                else BlockDownCollision();

                _timer = 0;
            }
            _timer++;

            // INPUT
            InputHandler(); // Call InputHandler
            _input = new ConsoleKeyInfo(); // Reset input var

            // RENDER CURRENT
            char[,] view = RenderView(); // Render view (Playing field)

            // RENDER HOLD
            char[,] hold = RenderHold(); // Render hold (the current held block)

            //RENDER UP NEXT
            char[,] next = RenderUpNext(); // Render the next three blocks as an 'up next' feature

            // PRINT VIEW
            Print(view, hold, next); // Print everything to the screen

            Thread.Sleep(20); // Wait to not overload the processor (I think it's better because it has no impact on game feel)
        }
    }

    static void InputHandler()
    {
        switch (_input.Key)
        {
            // Left arrow = move left (if it doesn't collide)
            case ConsoleKey.A:
            case ConsoleKey.LeftArrow:
                if (!Collision(_currentIndex, _bg, _currentX - 1, _currentY, _currentRot)) _currentX -= 1;
                break;

            // Right arrow = move right (if it doesn't collide)
            case ConsoleKey.D:
            case ConsoleKey.RightArrow:
                if (!Collision(_currentIndex, _bg, _currentX + 1, _currentY, _currentRot)) _currentX += 1;
                break;

            // Rotate block (if it doesn't collide)
            case ConsoleKey.W:
            case ConsoleKey.UpArrow:
                int newRot = _currentRot + 1;
                if (newRot >= 4) newRot = 0;
                if (!Collision(_currentIndex, _bg, _currentX, _currentY, newRot)) _currentRot = newRot;

                break;

            // Move the block instantly down (hard drop)
            case ConsoleKey.Spacebar:
                int i = 0;
                while (true)
                {
                    i++;
                    if (Collision(_currentIndex, _bg, _currentX, _currentY + i, _currentRot))
                    {
                        _currentY += i - 1;
                        break;
                    }

                }
                _score += i + 1;
                break;

            // Quit
            case ConsoleKey.Escape:
                Environment.Exit(1);
                break;

            // Hold block
            case ConsoleKey.Enter:

                // If there isnt a current held block:
                if (_holdIndex == -1)
                {
                    _holdIndex = _currentIndex;
                    _holdChar = _currentChar;
                    NewBlock();
                }
                // If there is:
                else
                if (!Collision(_holdIndex, _bg, _currentX, _currentY, 0))
                {
                    int c = _currentIndex;
                    char ch = _currentChar;
                    _currentIndex = _holdIndex;
                    _currentChar = _holdChar;
                    _holdIndex = c;
                    _holdChar = ch;
                }
                break;

            // Move down faster
            case ConsoleKey.S:
            case ConsoleKey.DownArrow:
                _timer = _maxTime;
                break;

            case ConsoleKey.R:
                Restart();
                break;

            default:
                break;
        }
    }
    static void BlockDownCollision()
    {

        // Add blocks from current to background
        for (int i = 0; i < _positions.GetLength(2); i++)
            _bg[_positions[_currentIndex, _currentRot, i, 1] + _currentY, _positions[_currentIndex, _currentRot, i, 0] + _currentX] = _currentChar;

        // Loop 
        while (true)
        {
            // Check for line
            int lineY = Line(_bg);

            // If a line is detected
            if (lineY != -1)
            {
                ClearLine(lineY);
                continue;
            }
            break;
        }
        // New block
        NewBlock();
    }

    static void Restart()
    {
        // Quite messy but it kinda works. Code by: KeremEskicinar
        var applicationPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
        Process.Start(applicationPath);
        Environment.Exit(Environment.ExitCode);
    }

    static void ClearLine(int lineY)
    {
        _score += 40;
        // Clear said line
        for (int x = 0; x < _mapSizeX; x++) _bg[lineY, x] = '-';

        // Loop through all blocks above line
        for (int y = lineY - 1; y > 0; y--)
            for (int x = 0; x < _mapSizeX; x++)
            {
                char character = _bg[y, x];
                if (character != '-')
                {
                    _bg[y, x] = '-';
                    _bg[y + 1, x] = character;
                }
            }
    }

    static char[,] RenderView()
    {
        char[,] view = new char[_mapSizeY, _mapSizeX];

        // Make view equal to bg
        for (int y = 0; y < _mapSizeY; y++)
            for (int x = 0; x < _mapSizeX; x++)
                view[y, x] = _bg[y, x];

        // Overlay current
        for (int i = 0; i < _positions.GetLength(2); i++)
            view[_positions[_currentIndex, _currentRot, i, 1] + _currentY, _positions[_currentIndex, _currentRot, i, 0] + _currentX] = _currentChar;

        return view;
    }

    static char[,] RenderHold()
    {
        char[,] hold = new char[_holdSizeY, _holdSizeX];
        // Hold = ' ' array
        for (int y = 0; y < _holdSizeY; y++)
            for (int x = 0; x < _holdSizeX; x++)
                hold[y, x] = ' ';

        // If there is a held block
        if (_holdIndex != -1)
            // Overlay blocks from hold
            for (int i = 0; i < _positions.GetLength(2); i++)
                hold[_positions[_holdIndex, 0, i, 1] + 1, _positions[_holdIndex, 0, i, 0] + 1] = _holdChar;

        return hold;
    }
    static char[,] RenderUpNext()
    {
        // Up next = ' ' array   
        char[,] next = new char[_mapSizeY, _upNextSize];
        for (int y = 0; y < _mapSizeY; y++)
            for (int x = 0; x < _upNextSize; x++)
                next[y, x] = ' ';

        int nextBagIndex = 0;
        for (int i = 0; i < 3; i++) // Next 3 blocks
        {

            for (int l = 0; l < _positions.GetLength(2); l++)
                if (i + _bagIndex >= 7) // If we need to acces the next bag
                    next[_positions[_nextBag![nextBagIndex], 0, l, 1] + 5 * i, _positions[_nextBag[nextBagIndex], 0, l, 0] + 1] = _characters[_nextBag[nextBagIndex]];
                else
                    next[_positions[_bag![_bagIndex + i], 0, l, 1] + 5 * i, _positions[_bag[_bagIndex + i], 0, l, 0] + 1] = _characters[_bag[_bagIndex + i]];

            if (i + _bagIndex >= 7) nextBagIndex++;
        }
        return next;
    }

    static void Print(char[,] view, char[,] hold, char[,] next)
    {
        for (int y = 0; y < _mapSizeY; y++)
        {

            for (int x = 0; x < _holdSizeX + _mapSizeX + _upNextSize; x++)
            {
                char i;
                // Add hold + Main View + up next to view (basically dark magic)
                if (x < _holdSizeX) i = hold[y, x];
                else if (x >= _holdSizeX + _mapSizeX) i = next[y, x - _mapSizeX - _upNextSize];
                else i = view[y, (x - _holdSizeX)];

                // Colours
                switch (i)
                {
                    case 'O':
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Write(i);
                        break;
                    case 'I':
                        Console.ForegroundColor = ConsoleColor.Blue;
                        Console.Write(i);
                        break;
                    case 'T':
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.Write(i);
                        break;
                    case 'S':
                        Console.ForegroundColor = ConsoleColor.DarkMagenta;
                        Console.Write(i);
                        break;
                    case 'Z':
                        Console.ForegroundColor = ConsoleColor.DarkCyan;
                        Console.Write(i);
                        break;
                    case 'L':
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write(i);
                        break;
                    case 'J':
                        Console.ForegroundColor = ConsoleColor.DarkCyan;
                        Console.Write(i);
                        break;
                    default:
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.Write(i);
                        break;
                }
            }
            if (y == 1)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write("   " + _score);
            }
            Console.WriteLine();
        }
        // Reset console cursor position
        Console.SetCursorPosition(0, Console.CursorTop - _mapSizeY);
    }

    static int[] GenerateBag()
    {
        // Not my code, source https://stackoverflow.com/questions/108819/best-way-to-randomize-an-array-with-net
        Random random = new();
        int n = 7;
        int[]? ret = { 0, 1, 2, 3, 4, 5, 6, 7 };
        while (n > 1)
        {
            int k = random.Next(n--);
            (ret[k], ret[n]) = (ret[n], ret[k]);
        }
        return ret;
    }

    static bool Collision(int index, char[,] bg, int x, int y, int rot)
    {
        for (int i = 0; i < _positions.GetLength(2); i++)
        {
            // Check if out of bounds
            if (_positions[index, rot, i, 1] + y >= _mapSizeY || _positions[index, rot, i, 0] + x < 0 || _positions[index, rot, i, 0] + x >= _mapSizeX)
                return true;

            // Check if not '-'
            if (bg[_positions[index, rot, i, 1] + y, _positions[index, rot, i, 0] + x] != '-')
                return true;
        }

        return false;
    }

    static int Line(char[,] bg)
    {
        for (int y = 0; y < _mapSizeY; y++)
        {
            bool i = true;
            for (int x = 0; x < _mapSizeX; x++)
                if (bg[y, x] == '-')
                    i = false;
            if (i) return y;
        }

        // If no line return -1
        return -1;
    }

    static void NewBlock()
    {
        // Check if new bag is necessary
        if (_bagIndex >= 7)
        {
            _bagIndex = 0;
            _bag = _nextBag;
            _nextBag = GenerateBag();
        }

        // Reset everything
        _currentY = 0;
        _currentX = 4;
        _currentChar = _characters[_bag![_bagIndex]];
        _currentIndex = _bag[_bagIndex];

        // Check if the next block position collides. If it does its gameover
        if (Collision(_currentIndex, _bg, _currentX, _currentY, _currentRot) && _amount > 0)
            GameOver();
        _bagIndex++;
        _amount++;
    }

    static void GameOver() =>
        // Possible restart functionality
        Environment.Exit(1);

    static void Input()
    {
        while (true)
            _input = Console.ReadKey(true);
    }
}