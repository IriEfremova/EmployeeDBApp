using System.Data;
using System.Text.RegularExpressions;

//Строка для соединения с БД
const string connectionString = "Server = MAMA-BOOK; Database = EmployeeDB; Trusted_Connection = True; Encrypt = false;";
int selectedMenuItem = 0; //Выбранный пункт меню
int lastRow = 0; //Последняя позиция курсора в консоли
bool isMainMenu = true; //Признак отображения главного меню или меню операции
string StatusMessage = ""; //Сообщение ою ошибке соединения с БД

//Пункты главного меню
string[] menuItems = [
    "Add new Employee",
    "Show All Employees",
    "Update Employee Information",
    "Delete Employee",
    "Exit" ];

//Поля таблицы
string[] fieldItems = [
    "FirstName",
    "LastName",
    "Email",
    "DateOfBirth",
    "Salary" ];

Console.WriteLine("Please, wait. Сonnecting to the database...");

//Соединяемся с БД
EmployeeDBApp.DBConnection dBConnection = new(connectionString);
StatusMessage = dBConnection.OpenConnection().Result;

//При необходимости можно добавить в базу хранимые процедуры
//dBConnection.CreateStoredProcedure();

//Чистим консоль от предыдущих сообщений и отображаем главное меню
Console.Clear();
ShowMainMenu(menuItems, selectedMenuItem);

//В бесконечном цикле обрабатываем нажатие клавиш пользователем
while (true)
{
    switch (Console.ReadKey(true).Key)
    {
        //Если стрелка вниз, то увеличиваем выбранный пункт и, если в главном меню, то перерисовываем его
        case ConsoleKey.DownArrow:
            if (selectedMenuItem < menuItems.Length - 1)
                selectedMenuItem++;
            if (isMainMenu)
                ShowMainMenu(menuItems, selectedMenuItem);
            break;
        //Если стрелка вверх, то уменьшаем выбранный пункт и, если в главном меню, то перерисовываем его
        case ConsoleKey.UpArrow:
            if (selectedMenuItem > 0)
                selectedMenuItem--;
            if (isMainMenu)
                ShowMainMenu(menuItems, selectedMenuItem);
            break;
        //Если клавиша Enter
        case ConsoleKey.Enter:
            //Если не в главном меню, значит выходим в него после выполенения операции
            if (isMainMenu == false)
            {
                Console.Clear();
                selectedMenuItem = 0;
                isMainMenu = true;
                ShowMainMenu(menuItems, selectedMenuItem);
            }
            //Если в главном меню
            else
            {
                //На пункт выхода выходим из программы
                if (selectedMenuItem == 4)
                {
                    dBConnection.CloseConnection();
                    return;
                }
                else
                {
                    SelectOperation(selectedMenuItem);
                }
            }
            break;
    }
}

//Отобразить главное меню (на вход - список пунктов и номер текущего пункта для выделения цветом)
void ShowMainMenu(string[] items, int index)
{
    Console.SetCursorPosition(0, 0);

    Console.WriteLine("Menu");
    Console.WriteLine();

    //Выводим список пунктов и номер текущего пункта выделяем цветом
    for (int i = 0; i < items.Length; i++)
    {
        if (i == index)
        {
            Console.BackgroundColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Black;
        }
        Console.WriteLine(items[i]);
        Console.ResetColor();
    }
    Console.WriteLine();

    //Чистим строку статусных сообщений от БД
    for (int i = 8; i < lastRow; i++)
    {
        Console.SetCursorPosition(0, i);
        Console.Write(new string(' ', Console.BufferWidth));
    }

    //Отображаем сообщения от БД
    Console.SetCursorPosition(0, 8);
    if (StatusMessage != null && StatusMessage.Length > 0)
        Console.WriteLine(StatusMessage);
    lastRow = Console.CursorTop;
}

//Отобразить информацию о выбранной операции
void ShowOptionMenu(int index)
{
    string message = string.Empty;
    Console.Clear();
    switch (index)
    {
        case 0: message = "Adding new Employee:"; break;
        case 1: message = "Show all Employees:"; break;
        case 2: message = "Update Employee information:"; break;
        case 3: message = "Deleting Employee:"; break;
    }
    Console.WriteLine(message);
}

//Выбор операции из главного меню
void SelectOperation(int index)
{
    if (dBConnection.ConnectionState() != ConnectionState.Open)
    {
        StatusMessage = "Operation cannot be performed! No connection to database.";
        ShowMainMenu(menuItems, selectedMenuItem);
    }
    else
    {
        isMainMenu = false;
        ShowOptionMenu(index);
        switch (index)
        {
            case 0: AddNewEmployee(); break;
            case 1: ShowAllEmployees(); break;
            case 2: UpdateEmployeeInfo(); break;
            case 3: DeleteEmployee(); break;
        }
    }
}

//Отобразить список всех сотрудников
void ShowAllEmployees()
{
    Console.WriteLine("Please wait...");
    List<string> res = dBConnection.ShowAllEmployees().Result;

    ClearIncorrectData(Console.CursorTop - 1);
    Console.WriteLine(string.Join("\n", res));
    Console.WriteLine("Press Enter to return in Main Menu");
}

//Очистить строки о введениии некорректных данных
void ClearIncorrectData(int cursorPosition)
{
    int currentCursor = Console.CursorTop;
    Console.SetCursorPosition(0, cursorPosition);
    for (int i = cursorPosition; i < currentCursor; i++)
    {
        Console.SetCursorPosition(0, i);
        Console.Write(new string(' ', Console.BufferWidth));
    }
    Console.SetCursorPosition(0, cursorPosition);
}

//Добавить нового пользователя
void AddNewEmployee()
{
    DateTime dateBirth;
    decimal salary;
    string res = "";
    bool isFirst = true;
    string message = "Enter the first name, last name, email address, date of birth" +
        " and salary of the employee, separated by a space.\n" +
        "For example: Ivan Ivanov ivanov@mail.ru 12.12.90 10000";

    do
    {
        Console.WriteLine(message);

        string info = Console.ReadLine().Trim();
        if (string.IsNullOrEmpty(info) == false)
        {
            //Парсим введенные данные и проверяем на корректность
            string[] listInfo = info.Split(" ");
            if (listInfo.Length == 5)
            {
                if (decimal.TryParse(listInfo[4], out salary))
                {
                    if (DateTime.TryParse(listInfo[3], out dateBirth))
                    {
                        if ((listInfo[0].Length <= 50 && Regex.IsMatch(listInfo[0], @"^[a-zA-Z]+$"))
                                        && (listInfo[1].Length <= 50 && Regex.IsMatch(listInfo[1], @"^[a-zA-Z]+$"))
                                        && (listInfo[2].Length <= 100 && listInfo[2].Contains("@")))
                        {
                            Console.WriteLine("Please wait...");
                            res = dBConnection.AddNewEmployee(listInfo[0], listInfo[1], listInfo[2], dateBirth, salary).Result;
                            break;
                        }
                    }
                }
            }
            if (isFirst)
            {
                message = "Please, input correct data. " + message;
                isFirst = false;
            }
        }
        ClearIncorrectData(1);
    } while (res == string.Empty);

    //Чистим сообщения о необходимости ввода данных и выводим результат
    ClearIncorrectData(1);
    Console.WriteLine();
    Console.WriteLine(res);
    Console.WriteLine();
    Console.WriteLine("Press Enter to return in Main Menu");
}

//Обновить информацию по сотруднику
void UpdateEmployeeInfo()
{
    string res = string.Empty; //Строка результата
    bool isFirst = true; //Если первый раз вводим некорректные данные
    string message = "Select Employee for update:"; //Начальное сообщение
    int selectItem = 0; //Текущий элемент списка для подсветки
    int currentEmployee = -1; //ID выбранного для изменения сотрудника
    string currentField = string.Empty; //Выбранное поле для изменения
    bool isFinishUpdate = false; //Настройка выбора информации для изменения завершена

    Console.WriteLine("Please wait...");

    List<string> list = dBConnection.ShowAllEmployees().Result;

    Console.SetCursorPosition(0, 1);
    Console.WriteLine(message);

    //Если таблица пользователей не пустая
    if (list != null && list.Count > 0 && list[0].Contains("table Employees") == false)
    {
        //Пока не введены корректные данные
        while (isFinishUpdate == false)
        {
            int cnt = list.Count;
            //Если не выбран сотрудник для изменения, то показваем список сотрудников, иначе список полей таблицы
            if (currentEmployee == -1)
            {
                ShowSubMenu(list.ToArray(), selectItem);
            }
            else
            {
                ShowSubMenu(fieldItems, selectItem);
                cnt = fieldItems.Length;
            }

            //Проверяем нажатую клавишу и обрабатываем ее нажатие
            switch (Console.ReadKey(true).Key)
            {
                case ConsoleKey.DownArrow:
                    if (selectItem < cnt - 1)
                        selectItem++;
                    break;
                case ConsoleKey.UpArrow:
                    if (selectItem > 0)
                        selectItem--;
                    break;
                case ConsoleKey.Enter:
                    //Если сотрудник не выбран, выбираем
                    if (currentEmployee == -1)
                    {
                        //Парсим из текущей строки ID
                        string str = list[selectItem];
                        int ind = str.IndexOf(" ");
                        str = str.Substring(0, ind);
                        if (int.TryParse(str, out currentEmployee))
                        {
                            selectItem = 0;
                            //Чистим предыдущее сообщение
                            ClearIncorrectData(1);
                            //Выводим новый этап операции
                            Console.WriteLine("Select Field for Update:");
                        }
                    }
                    else
                    {
                        //Если сотрудник выбран, то значит этап выбора поля таблицы
                        currentField = fieldItems[selectItem];
                        ClearIncorrectData(1);
                        //Переходим к вводу значения
                        message = $"Input new value for field {currentField}:";
                        do
                        {
                            Console.SetCursorPosition(0, 1);
                            Console.WriteLine(message);

                            string value = Console.ReadLine();
                            if (value != null && value.Length > 0)
                            {
                                Console.WriteLine("Please wait...");
                                //Парсим введенные данные и проверяем на корректность
                                switch (selectItem)
                                {
                                    case 0:
                                        if (value.Length <= 50 && Regex.IsMatch(value, @"^[a-zA-Z]+$"))
                                            res = dBConnection.UpdateEmployeeInfo(currentField, currentEmployee, firstName: value).Result;
                                        break;
                                    case 1:
                                        if (value.Length <= 50 && Regex.IsMatch(value, @"^[a-zA-Z]+$"))
                                            res = dBConnection.UpdateEmployeeInfo(currentField, currentEmployee, lastName: value).Result;
                                        break;
                                    case 2:
                                        if (value.Length <= 100 && value.Contains("@"))
                                            res = dBConnection.UpdateEmployeeInfo(currentField, currentEmployee, email: value).Result;
                                        break;
                                    case 3:
                                        if (DateTime.TryParse(value, out DateTime dateBirth))
                                            res = dBConnection.UpdateEmployeeInfo(currentField, currentEmployee, dateOfBirth: dateBirth).Result;
                                        break;
                                    case 4:
                                        if (decimal.TryParse(value, out decimal salary))
                                            res = dBConnection.UpdateEmployeeInfo(currentField, currentEmployee, salary: salary).Result;
                                        break;
                                }
                            }
                            if (isFirst)
                            {
                                message = "Please, input correct data. " + message;
                                Console.Write(new string(' ', Console.BufferWidth));
                                isFirst = false;
                            }
                            ClearIncorrectData(1);
                        } while (res == string.Empty);
                        isFinishUpdate = true;
                    }
                    break;
            }
        }
    }
    else
    {
        res = "No data in table Employees";
    }

    //Чистим сообщения о необходимости ввода данных и выводим результат
    ClearIncorrectData(1);
    Console.WriteLine();
    Console.WriteLine(res);
    Console.WriteLine();
    Console.WriteLine("Press Enter to return in Main Menu");
}

//Отобразить подменю операции изменения данных (список сотрудников или спиоск полей)
void ShowSubMenu(string[] items, int index)
{
    Console.SetCursorPosition(0, 2);
    for (int i = 0; i < items.Length; i++)
    {
        if (i == index)
        {
            Console.BackgroundColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Black;
        }
        Console.WriteLine(items[i]);
        Console.ResetColor();
    }
    Console.WriteLine();
}

//Удаление записи сотрудника из БД
void DeleteEmployee()
{
    string res = "";
    bool isFirst = true;
    string message = "Enter Employee id for delete";

    do
    {
        Console.WriteLine(message);
        //Считываем ID
        string info = Console.ReadLine();
        if (info != null && info.Length > 0)
        {
            //Проверяем на корректность
            if (int.TryParse(info, out int idEmployee))
            {
                Console.WriteLine("Please wait...");
                res = dBConnection.DeleteEmployee(idEmployee).Result;
                break;
            }
        }
        if (isFirst)
        {
            message = "Please, input correct data. " + message;
            isFirst = false;
        }
        ClearIncorrectData(1);
    } while (res == string.Empty);

    //Чистим сообщения о необходимости ввода данных и выводим результат
    ClearIncorrectData(1);
    Console.WriteLine();
    Console.WriteLine(res);
    Console.WriteLine();
    Console.WriteLine("Press Enter to return in Main Menu");
}

//Надо реализовать - отмена текущей операции
bool CheckReturnMainMenu()
{
    var button = Console.ReadKey();
    if (button.Key == ConsoleKey.Enter)
    {
        Console.Clear();
        isMainMenu = true;
        ShowMainMenu(menuItems, selectedMenuItem);
        return true;
    }
    return false;
}