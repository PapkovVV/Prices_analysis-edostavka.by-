using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FullControls.Controls;
using FullControls.SystemComponents;
using KursovayaDB.DataBaseServices;
using KursovayaDB.Services.DataBaseServices;
using KursovayaDB.Services.LogServices;
using KursovayaDB.Services.ParsingServices;
using KursovayaDB.Services.SecurityServices;
using KursovayaDB.Views;
using System.Windows;
namespace KursovayaDB.ViewModel;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty] string loginResgistrationTittle = "Вход";
    [ObservableProperty] string loginResgistrationButtonText = "Войти";
    [ObservableProperty] string hyperlinkText = "Нет аккаунта? Создайте";
    [ObservableProperty] static string logText;

    [ObservableProperty] string login;//Логин пользователя
    PasswordBoxPlus passwordBox;
    [ObservableProperty] string password;//Пароль пользователя

    [ObservableProperty] bool isAuthButtonEnable;//Возможность нажатия кнопки авторизации
    [ObservableProperty] bool isProgressBarVisible;//Видимость ProgressBar

    public MainViewModel(PasswordBoxPlus passwordBox)
    {
        InitializeAsync();
        this.passwordBox=passwordBox;
    }

    async void InitializeAsync()//Инициализация компонентов(OP)
    {
        try
        {
            IsAuthButtonEnable = false;
            IsProgressBarVisible = true;

            await LogFile.Create();//Создание Log-файла
            await SQLScripts.CreateDatabaseAsync();//Выполнение операций с БД
            await UpdateValues();//Выполнение парсинга и обновление БД, при 
        }
        finally
        {
            IsProgressBarVisible = false;
            IsAuthButtonEnable = true;
        }
    }

    async Task UpdateValues()//Выполнение парсинга и обновление БД, при необходимости(OP)
    {
        DateTime? dateTime = await SQLScripts.GetLastDate();
        if (dateTime == null || dateTime?.Date < DateTime.Now.Date)
        {
            await Parsing.ParseAllAsync(this);//Парсинг продуктов
            await UpdateDB.UpdateDataBase();//Обновление данных БД
        }
    }


    [RelayCommand]
    void GoToSingUp()//Переход к регистрации(OP)
    {
        if (LoginResgistrationTittle.Equals("Вход"))
        {
            SetContents(string.Empty, string.Empty, "Регистрация", "Зарегистрироваться", "У меня уже есть аккаунт");
        }
        else
        {
            SetContents(string.Empty, string.Empty, "Вход", "Войти", "Нет аккаунта? Создайте");
        }
    }

    [RelayCommand]
    async Task SingUp_LoginInAsync()//Выполнение регистрации/входа(OP)
    {
        if (string.IsNullOrEmpty(Login) || string.IsNullOrEmpty(Password))
        {
            MessageBox.Show("Все поля должны быть заполнены!");
        }
        else
        {
            string? login = Login.Trim();
            string? password = Password.Trim();
            int userCheckResult = await SQLScripts.CheckUserAsync(login);//Проверка существования пользователя в системе

            if (loginResgistrationTittle.Equals("Регистрация"))
            {
                if (userCheckResult == 0)
                {
                    if (await SQLScripts.AddUserAsync(login, HashPassword.Hash_Password(password)) == 1)
                    {
                        MessageBox.Show($"Пользователь {login} успешно добавлен!");
                        SetContents(login, password, "Вход", "Войти", "Нет аккаунта? Создайте");
                    }
                }
                else
                {
                    MessageBox.Show($"Пользователь {login} уже есть в базе!");
                }
            }
            else
            {
                if (userCheckResult  == 0) MessageBox.Show($"Пользователь {login} не существует!");
                else
                {
                    var hashPassword = await SQLScripts.GetUserHashPassword(login);

                    if (HashPassword.VerifyPassword(password, hashPassword))
                    {
                        FullWindow welcomeWindow = new WelcomeWindow
                        {
                            Title = $"Добро пожаловать, {login}"
                        };
                        Application.Current.MainWindow.Close();
                        welcomeWindow.Show();
                        welcomeWindow.Height +=1;
                    }
                    else
                    {
                        MessageBox.Show("Пароль неверный!");
                    }
                }
            }

        }

        /* FullWindow welcomeWindow = new WelcomeWindow
         {
             Title = $"Добро пожаловать, {login}"
         };
         Application.Current.MainWindow.Close();
         welcomeWindow.Show();
         welcomeWindow.Height +=1;*/

    }

    void SetContents(string login, string password, string title, string buttonContent, string hyperlinkText)//Установка значений текста в интерфейсе(OP)
    {
        Login = login;
        passwordBox.Password = password;
        LoginResgistrationTittle = title;
        LoginResgistrationButtonText = buttonContent;
        HyperlinkText = hyperlinkText;
    }

}
