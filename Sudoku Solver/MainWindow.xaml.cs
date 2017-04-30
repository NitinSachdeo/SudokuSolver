using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Sudoku_Solver
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static Button selected;

        public static char[,] grid = new char[9, 9], gridSave = new char[9,9];
        public static List<int>[,] spaces = new List<int>[9, 9], spacesSave = new List<int>[9, 9];

        public MainWindow()
        {
            InitializeComponent();
            WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
        }

        public void SelectCell(object sender, EventArgs e)
        {
            Sudoku.IsEnabled = true;
            Functions.IsEnabled = true;

            if (selected != null)
                selected.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 79, 9, 9));

            selected = (Button)sender;

            selected.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 22, 124, 50));
        }

        public void SetValue(object sender, MouseButtonEventArgs e)
        {
            Button pressed = (Button)sender;
            bool flag = false;

            pressed.FontSize = 30;
            if (pressed.Content != selected.Content)
                pressed.Content = selected.Content;
            else
                pressed.Content = " ";

            foreach (Button cell in Sudoku.Children)
                if (CheckForConflicts(int.Parse(cell.Tag.ToString()[0].ToString()), int.Parse(cell.Tag.ToString()[1].ToString())))
                    flag = true;

            Solve.IsEnabled = !flag;
        }


        private void KeyPress(object sender, KeyEventArgs e)
        {
            if (Choices.IsEnabled)
                switch (e.Key)
                {
                    case Key.NumPad1:
                    case Key.D1:
                        SelectCell(Add1, e);
                        break;

                    case Key.NumPad2:
                    case Key.D2:
                        SelectCell(Add2, e);
                        break;

                    case Key.NumPad3:
                    case Key.D3:
                        SelectCell(Add3, e);
                        break;

                    case Key.NumPad4:
                    case Key.D4:
                        SelectCell(Add4, e);
                        break;

                    case Key.NumPad5:
                    case Key.D5:
                        SelectCell(Add5, e);
                        break;

                    case Key.NumPad6:
                    case Key.D6:
                        SelectCell(Add6, e);
                        break;

                    case Key.NumPad7:
                    case Key.D7:
                        SelectCell(Add7, e);
                        break;

                    case Key.NumPad8:
                    case Key.D8:
                        SelectCell(Add8, e);
                        break;

                    case Key.NumPad9:
                    case Key.D9:
                        SelectCell(Add9, e);
                        break;
                }
        }

        public bool CheckForConflicts(int row, int column)
        {
            bool conflicts = false;

            ReadData();

            if (char.IsNumber(grid[row, column]))
            {
                for (int x = (row / 3) * 3; x < ((row / 3) * 3) + 3; x++)
                    for (int y = (column / 3) * 3; y < ((column / 3) * 3) + 3; y++)
                        if (grid[row, column] == grid[x, y] && (row != x || column != y))
                            HighlightError(Tuple.Create<Tuple<int, int>, Tuple<int, int>>(Tuple.Create<int, int>(row, column), Tuple.Create<int, int>(x, y)), ref conflicts);

                for (int i = 0; i < 9; i++)
                    if (grid[row, column] == grid[i, column] && row != i)
                        HighlightError(Tuple.Create<Tuple<int, int>, Tuple<int, int>>(Tuple.Create<int, int>(row, column), Tuple.Create<int, int>(i, column)), ref conflicts);

                for (int i = 0; i < 9; i++)
                    if (grid[row, column] == grid[row, i] && i != column)
                        HighlightError(Tuple.Create<Tuple<int, int>, Tuple<int, int>>(Tuple.Create<int, int>(row, column), Tuple.Create<int, int>(row, i)), ref conflicts);
            }

            if (!conflicts)
                FindChild<Button>(Sudoku, String.Format("Cell{0}_{1}", row, column)).Foreground = new SolidColorBrush(Colors.Black);

            return conflicts;
        }

        public void HighlightError(Tuple<Tuple<int, int>, Tuple<int, int>> input, ref bool flag)
        {
            List<Tuple<int, int>> pair = new List<Tuple<int, int>>();

            pair.Add(input.Item1);
            pair.Add(input.Item2);

            foreach (Tuple<int, int> set in pair)
                FindChild<Button>(Sudoku, String.Format("Cell{0}_{1}", set.Item1, set.Item2)).Foreground = new SolidColorBrush(Colors.Red);

            flag = true;
        }

        private void SolveClick(object sender, RoutedEventArgs e)
        {
            #region Parsing
            Reset();

            ReadData();
            #endregion

            #region Pencil
            for (int x = 0; x < 9; x++)
            {
                for (int y = 0; y < 9; y++)
                {
                    #region Setup
                    int box = ((x / 3) * 3) + (y / 3);

                    if (grid[x, y] != ' ')
                    {
                        spaces[x, y].Add(int.Parse(grid[x, y].ToString()));
                        continue;
                    }
                    for (int i = 1; i <= 9; i++)
                        spaces[x, y].Add(i);
                    #endregion

                    #region Pencil by Box   
                    for (int a = (box / 3) * 3; a < ((box / 3) * 3) + 3; a++)
                        for (int b = (box % 3) * 3; b < ((box % 3) * 3) + 3; b++)
                            if (grid[a, b] != ' ')
                                spaces[x, y].Remove(int.Parse(grid[a, b].ToString()));
                    #endregion

                    #region Pencil by Row
                    for (int i = 0; i < 9; i++)
                        if (grid[x, i] != ' ')
                            spaces[x, y].Remove(int.Parse(grid[x, i].ToString()));
                    #endregion

                    #region Pencil by Column
                    for (int i = 0; i < 9; i++)
                        if (grid[i, y] != ' ')
                            spaces[x, y].Remove(int.Parse(grid[i, y].ToString()));
                    #endregion
                }
            }
            #endregion

            #region Solving
            Solving();

            if (Check())
            {
                #region Brute Forceing
                spacesSave = GenericCopier<List<int>[,]>.DeepCopy(spaces);
                gridSave = GenericCopier<char[,]>.DeepCopy(grid);

                for (int x = 0; x < 9; x++)
                    for (int y = 0; y < 9; y++)
                        if (spaces[x, y].Count > 1)
                            for (int i = 0; i < spaces[x, y].Count; i++)
                                if (BruteForce(x, y, i))
                                    goto Exit;
                #endregion
            }
            Exit:
            #endregion

            #region Display        
            Display();
            Sudoku.IsEnabled = false;
            Choices.IsEnabled = false;

            if (Check())
                MessageBox.Show("Sorry, this is the best I could do.");
            else
                MessageBox.Show("Solution Found!");
            #endregion
        }

        private void ClearClick(object sender, RoutedEventArgs e)
        {
            Reset();

            foreach (Button cell in Sudoku.Children)
                cell.Content = " ";

            Sudoku.IsEnabled = true;
            Choices.IsEnabled = true;
        }

        public void ReadData()
        {
            for (int x = 0; x < 9; x++)
                for (int y = 0; y < 9; y++)
                {
                    if (FindChild<Button>(Sudoku, String.Format("Cell{0}_{1}", x, y)).FontSize != 30)
                        FindChild<Button>(Sudoku, String.Format("Cell{0}_{1}", x, y)).Content = " ";

                    grid[x, y] = FindChild<Button>(Sudoku, String.Format("Cell{0}_{1}", x, y)).Content.ToString()[0];
                    spaces[x, y] = new List<int>();
                }
        }

        static void Solving()
        {
            int attempts = 0;

            while (Check() && attempts < 10)
            {
                attempts++;

                #region Simple Solving
                for (int x = 0; x < 9; x++)
                    for (int y = 0; y < 9; y++)
                        CheckForSingles(x, y);

                for (int i = 0; i < 9; i++)
                {
                    CheckBox(i);
                    CheckRow(i);
                    CheckColumn(i);
                }
                #endregion

                if (!Check())
                    break;

                #region FindNakedPairs
                #region Box
                for (int box = 0; box < 9; box++)
                    for (int a = (box / 3) * 3; a < ((box / 3) * 3) + 3; a++)
                        for (int b = (box % 3) * 3; b < ((box % 3) * 3) + 3; b++)
                            if (spaces[a, b].Count == 2)
                                for (int c = (box / 3) * 3; c < ((box / 3) * 3) + 3; c++)
                                    for (int d = (box % 3) * 3; d < ((box % 3) * 3) + 3; d++)
                                        if (new HashSet<int>(spaces[a, b]).SetEquals(spaces[c, d]) && (a != c || b != d))
                                            FoundNakedPair("Box", box, Tuple.Create<int, int>(a, b), Tuple.Create<int, int>(c, d), spaces[a, b]);
                #endregion

                #region Row
                for (int row = 0; row < 9; row++)
                    for (int a = 0; a < 9; a++)
                        if (spaces[row, a].Count == 2)
                            for (int b = 0; b < 9; b++)
                                if (new HashSet<int>(spaces[row, a]).SetEquals(spaces[row, b]) && a != b)
                                    FoundNakedPair("Row", row, Tuple.Create<int, int>(row, a), Tuple.Create<int, int>(row, b), spaces[row, a]);
                #endregion

                #region Column
                for (int column = 0; column < 9; column++)
                    for (int a = 0; a < 9; a++)
                        if (spaces[a, column].Count == 2)
                            for (int b = 0; b < 9; b++)
                                if (new HashSet<int>(spaces[a, column]).SetEquals(spaces[b, column]) && a != b)
                                    FoundNakedPair("Column", column, Tuple.Create<int, int>(a, column), Tuple.Create<int, int>(b, column), spaces[a, column]);
                #endregion
                #endregion

                //TODO
                #region FindNakedTriplets

                #endregion

                #region FindNakedQuads

                #endregion

                #region Omission

                #endregion

                #region X Wing

                #endregion
            }
        }

        static void CheckForSingles(int x, int y)
        {
            if (spaces[x, y].Count == 1)
            {
                grid[x, y] = spaces[x, y][0].ToString()[0];

                RemoveNeighbours(x, y, spaces[x, y][0]);
            }
        }

        static void CheckBox(int box)
        {
            Tuple<int, int> found = new Tuple<int, int>(-1, -1);

            for (int num = 1; num <= 9; num++)
            {
                int count = 0;

                for (int a = (box / 3) * 3; a < ((box / 3) * 3) + 3; a++)
                    for (int b = (box % 3) * 3; b < ((box % 3) * 3) + 3; b++)
                        if (spaces[a, b].Contains(num))
                        {
                            found = new Tuple<int, int>(a, b);

                            count++;
                        }

                if (count == 1)
                    RemoveNeighbours(found.Item1, found.Item2, num);
            }
        }

        static void CheckRow(int row)
        {
            Tuple<int, int> found = new Tuple<int, int>(-1, -1);

            for (int num = 1; num <= 9; num++)
            {
                int count = 0;

                for (int a = 0; a < 9; a++)
                    if (spaces[row, a].Contains(num))
                    {
                        found = new Tuple<int, int>(row, a);

                        count++;
                    }

                if (count == 1)
                    RemoveNeighbours(found.Item1, found.Item2, num);
            }
        }

        static void CheckColumn(int column)
        {
            Tuple<int, int> found = new Tuple<int, int>(-1, -1);

            for (int num = 1; num <= 9; num++)
            {
                int count = 0;

                for (int a = 0; a < 9; a++)
                    if (spaces[a, column].Contains(num))
                    {
                        found = new Tuple<int, int>(a, column);

                        count++;
                    }

                if (count == 1)
                    RemoveNeighbours(found.Item1, found.Item2, num);
            }
        }

        static void RemoveNeighbours(int x, int y, int remove)
        {
            int box = ((x / 3) * 3) + (y / 3);

            spaces[x, y].Clear();

            grid[x, y] = remove.ToString()[0];

            for (int a = (box / 3) * 3; a < ((box / 3) * 3) + 3; a++)
                for (int b = (box % 3) * 3; b < ((box % 3) * 3) + 3; b++)
                    spaces[a, b].Remove(remove);

            for (int i = 0; i < 9; i++)
                spaces[x, i].Remove(remove);

            for (int i = 0; i < 9; i++)
                spaces[i, y].Remove(remove);

            spaces[x, y].Add(remove);
        }

        static void FoundNakedPair(string group, int index, Tuple<int, int> firstPair, Tuple<int, int> secondPair, List<int> remove)
        {
            switch (group)
            {
                case "Box":
                    for (int a = (index / 3) * 3; a < ((index / 3) * 3) + 3; a++)
                        for (int b = (index % 3) * 3; b < ((index % 3) * 3) + 3; b++)
                            if (!firstPair.Equals(Tuple.Create<int, int>(a, b)) && !secondPair.Equals(Tuple.Create<int, int>(a, b)))
                                spaces[a, b] = spaces[a, b].Except(remove).ToList();
                    break;
                case "Row":
                    for (int a = 0; a < 9; a++)
                        if (!firstPair.Equals(Tuple.Create<int, int>(index, a)) && !secondPair.Equals(Tuple.Create<int, int>(index, a)))
                            spaces[index, a] = spaces[index, a].Except(remove).ToList();
                    break;
                case "Column":
                    for (int a = 0; a < 9; a++)
                        if (!firstPair.Equals(Tuple.Create<int, int>(a, index)) && !secondPair.Equals(Tuple.Create<int, int>(a, index)))
                            spaces[a, index] = spaces[a, index].Except(remove).ToList();
                    break;
            }
        }

        static bool BruteForce(int x, int y, int trialValue)
        {
            RemoveNeighbours(x, y, spaces[x, y][trialValue]);

            Solving();

            if (Check())
            {
                spaces = GenericCopier<List<int>[,]>.DeepCopy(spacesSave);
                grid = GenericCopier<char[,]>.DeepCopy(gridSave);

                return false;
            }
            else
                return true;
        }

        static bool Check()
        {
            foreach (char space in grid)
                if (!char.IsNumber(space))
                    return true;
            return false;
        }

        public void Display()
        {
            for (int x = 0; x < 9; x++)
                for (int y = 0; y < 9; y++)
                    if (spaces[x, y].Count == 1)
                    {
                        FindChild<Button>(Sudoku, String.Format("Cell{0}_{1}", x, y)).FontSize = 30;
                        FindChild<Button>(Sudoku, String.Format("Cell{0}_{1}", x, y)).Content = spaces[x, y][0];
                    }
                    else
                    {
                        FindChild<Button>(Sudoku, String.Format("Cell{0}_{1}", x, y)).FontSize = Application.Current.MainWindow.Height / 100;
                        FindChild<Button>(Sudoku, String.Format("Cell{0}_{1}", x, y)).Content = "";

                        for (int count = 1; count <= spaces[x, y].Count; count++)
                        {
                            FindChild<Button>(Sudoku, String.Format("Cell{0}_{1}", x, y)).Content += spaces[x, y][count - 1] + " ";

                            if (count % 3 == 0)
                                FindChild<Button>(Sudoku, String.Format("Cell{0}_{1}", x, y)).Content += "\n";
                        }
                    }
        }

        public void Reset()
        {
            for (int x = 0; x < 9; x++)
                for (int y = 0; y < 9; y++)
                {
                    grid[x, y] = ' ';

                    if (spaces[x, y] != null)
                        spaces[x, y].Clear();
                }
        }

        #region Functions Obtained Online
        /// <summary>
        /// Finds a Child of a given item in the visual tree. 
        /// </summary>
        /// <param name="parent">A direct parent of the queried item.</param>
        /// <typeparam name="T">The type of the queried item.</typeparam>
        /// <param name="childName">x:Name or Name of child. </param>
        /// <returns>The first parent item that matches the submitted type parameter. 
        /// If not matching item can be found, 
        /// a null parent is being returned.</returns>
        public static T FindChild<T>(DependencyObject parent, string childName)
           where T : DependencyObject
        {
            // Confirm parent and childName are valid. 
            if (parent == null) return null;

            T foundChild = null;

            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                // If the child is not of the request child type child
                T childType = child as T;
                if (childType == null)
                {
                    // recursively drill down the tree
                    foundChild = FindChild<T>(child, childName);

                    // If the child is found, break so we do not overwrite the found child. 
                    if (foundChild != null) break;
                }
                else if (!string.IsNullOrEmpty(childName))
                {
                    var frameworkElement = child as FrameworkElement;
                    // If the child's name is set for search
                    if (frameworkElement != null && frameworkElement.Name == childName)
                    {
                        // if the child's name is of the request name
                        foundChild = (T)child;
                        break;
                    }
                }
                else
                {
                    // child element found.
                    foundChild = (T)child;
                    break;
                }
            }

            return foundChild;
        }

        public static class GenericCopier<T>    //deep copy a list
        {
            public static T DeepCopy(object objectToCopy)
            {
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    BinaryFormatter binaryFormatter = new BinaryFormatter();
                    binaryFormatter.Serialize(memoryStream, objectToCopy);
                    memoryStream.Seek(0, SeekOrigin.Begin);
                    return (T)binaryFormatter.Deserialize(memoryStream);
                }
            }
        }
        #endregion
    }
}