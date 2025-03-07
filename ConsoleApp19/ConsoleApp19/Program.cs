﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace TaskManagerApp
{
    public enum Priority { Низкий, Средний, Высокий }
    public enum Status { Недоступна, ВПроцессе, Завершена }

    public class TaskItem
    {
        public string Заголовок { get; set; }
        public string Описание { get; set; }
        public Priority Приоритет { get; set; }
        public Status Статус { get; set; }
        public string Пользователь { get; set; }

        public override string ToString() => $"{Заголовок}|{Описание}|{Приоритет}|{Статус}|{Пользователь}";

        public static TaskItem FromString(string s)
        {
            var p = s.Split('|');
            return new TaskItem
            {
                Заголовок = p[0],
                Описание = p[1],
                Приоритет = (Priority)Enum.Parse(typeof(Priority), p[2]),
                Статус = (Status)Enum.Parse(typeof(Status), p[3]),
                Пользователь = p[4]
            };
        }
    }

    public class Program
    {
        private static readonly string UsersFile = "users.txt";
        private static readonly string TasksFile = "tasks.txt";

        public static async Task Main(string[] args)
        {
            Console.WriteLine("Менеджер задач");
            while (true)
            {
                Console.WriteLine("\nВыберите действие:");
                Console.WriteLine("1. Регистрация");
                Console.WriteLine("2. Вход");
                Console.WriteLine("3. Выход");

                switch (Console.ReadLine())
                {
                    case "1": await Register(); break;
                    case "2":
                        var user = await Login();
                        if (user != null) await TaskMenu(user);
                        break;
                    case "3": Console.WriteLine("Выход..."); return;
                    default: Console.WriteLine("Неверный выбор."); break;
                }
            }
        }

        private static async Task Register()
        {
            Console.Write("Имя пользователя: ");
            string u = Console.ReadLine();
            Console.Write("Пароль: ");
            string p = Console.ReadLine();

            if (await UserExists(u))
            {
                Console.WriteLine("Пользователь существует.");
                return;
            }

            try
            {
                await File.AppendAllTextAsync(UsersFile, $"{u}|{p}\n");
                Console.WriteLine("Регистрация успешна!");
            }
            catch (Exception e) { Console.WriteLine($"Ошибка регистрации: {e.Message}"); }
        }

        private static async Task<string> Login()
        {
            Console.Write("Имя пользователя: ");
            string u = Console.ReadLine();
            Console.Write("Пароль: ");
            string p = Console.ReadLine();

            try
            {
                foreach (var line in await File.ReadAllLinesAsync(UsersFile))
                {
                    var parts = line.Split('|');
                    if (parts[0] == u && parts[1] == p)
                    {
                        Console.WriteLine($"Вход выполнен как {u}!");
                        return u;
                    }
                }
                Console.WriteLine("Неверное имя пользователя или пароль.");
                return null;
            }
            catch (Exception e) { Console.WriteLine($"Ошибка входа: {e.Message}"); return null; }
        }

        private static async Task TaskMenu(string user)
        {
            while (true)
            {
                Console.WriteLine("\nМеню задач:");
                Console.WriteLine("1. Создать задачу");
                Console.WriteLine("2. Просмотреть задачи");
                Console.WriteLine("3. Редактировать задачу");
                Console.WriteLine("4. Удалить задачу");
                Console.WriteLine("5. Выход");

                switch (Console.ReadLine())
                {
                    case "1": await CreateTask(user); break;
                    case "2": await ViewTasks(user); break;
                    case "3": await EditTask(user); break;
                    case "4": await DeleteTask(user); break;
                    case "5": return;
                    default: Console.WriteLine("Неверный выбор."); break;
                }
            }
        }

        private static async Task CreateTask(string user)
        {
            Console.Write("Заголовок: ");
            string t = Console.ReadLine();
            Console.Write("Описание: ");
            string d = Console.ReadLine();

            Console.WriteLine("Приоритет (Низкий, Средний, Высокий): ");
            if (!Enum.TryParse(Console.ReadLine(), true, out Priority p)) p = Priority.Средний;

            Console.WriteLine("Статус (Недоступна, В Процессе, Завершена): ");
            if (!Enum.TryParse(Console.ReadLine(), true, out Status s)) s = Status.Недоступна;

            var task = new TaskItem
            {
                Заголовок = t,
                Описание = d,
                Приоритет = p,
                Статус = s,
                Пользователь = user
            };

            try
            {
                await File.AppendAllTextAsync(TasksFile, task + "\n");
                Console.WriteLine("Задача создана!");
            }
            catch (Exception e) { Console.WriteLine($"Ошибка создания задачи: {e.Message}"); }
        }

        private static async Task ViewTasks(string user)
        {
            try
            {
                var tasks = await LoadTasks(user);
                if (tasks.Count == 0) { Console.WriteLine("Задачи не найдены."); return; }
                Console.WriteLine("\nВаши задачи:");
                for (int i = 0; i < tasks.Count; i++)
                {
                    Console.WriteLine($"{i + 1}. {tasks[i].Заголовок} - {tasks[i].Статус} - {tasks[i].Приоритет}");
                }
            }
            catch (Exception e) { Console.WriteLine($"Ошибка просмотра задач: {e.Message}"); }
        }

        private static async Task EditTask(string user)
        {
            var tasks = await LoadTasks(user);
            if (tasks.Count == 0) { Console.WriteLine("Нет задач для редактирования."); return; }

            Console.WriteLine("Выберите задачу для редактирования (номер):");
            for (int i = 0; i < tasks.Count; i++)
                Console.WriteLine($"{i + 1}. {tasks[i].Заголовок} - {tasks[i].Статус} - {tasks[i].Приоритет}");

            if (!int.TryParse(Console.ReadLine(), out int index) || index < 1 || index > tasks.Count)
            {
                Console.WriteLine("Неверный номер задачи.");
                return;
            }

            var task = tasks[index - 1];

            Console.Write("Новый заголовок (оставьте пустым для текущего): ");
            string nt = Console.ReadLine();
            if (!string.IsNullOrEmpty(nt)) task.Заголовок = nt;

            Console.Write("Новое описание (оставьте пустым для текущего): ");
            string nd = Console.ReadLine();
            if (!string.IsNullOrEmpty(nd)) task.Описание = nd;

            Console.Write("Новый приоритет (Низкий, Средний, Высокий, оставьте пустым для текущего): ");
            string np = Console.ReadLine();
            if (!string.IsNullOrEmpty(np) && Enum.TryParse(np, true, out Priority newP)) task.Приоритет = newP;

            Console.Write("Новый статус (Недоступна, В Процессе, Завершена, оставьте пустым для текущего): ");
            string ns = Console.ReadLine();
            if (!string.IsNullOrEmpty(ns) && Enum.TryParse(ns, true, out Status newS)) task.Статус = newS;

            try
            {
                await SaveTasks(tasks, user);
                Console.WriteLine("Задача обновлена!");
            }
            catch (Exception e) { Console.WriteLine($"Ошибка редактирования задачи: {e.Message}"); }
        }

        private static async Task DeleteTask(string user)
        {
            var tasks = await LoadTasks(user);
            if (tasks.Count == 0) { Console.WriteLine("Нет задач для удаления."); return; }

            Console.WriteLine("Выберите задачу для удаления (номер):");
            for (int i = 0; i < tasks.Count; i++)
                Console.WriteLine($"{i + 1}. {tasks[i].Заголовок} - {tasks[i].Статус} - {tasks[i].Приоритет}");

            if (!int.TryParse(Console.ReadLine(), out int index) || index < 1 || index > tasks.Count)
            {
                Console.WriteLine("Неверный номер задачи.");
                return;
            }

            tasks.RemoveAt(index - 1);

            try
            {
                await SaveTasks(tasks, user);
                Console.WriteLine("Задача удалена!");
            }
            catch (Exception e) { Console.WriteLine($"Ошибка удаления задачи: {e.Message}"); }
        }

        private static async Task<List<TaskItem>> LoadTasks(string user)
        {
            var tasks = new List<TaskItem>();
            try
            {
                if (!File.Exists(TasksFile)) return tasks;
                foreach (var line in await File.ReadAllLinesAsync(TasksFile))
                {
                    var task = TaskItem.FromString(line);
                    if (task.Пользователь == user) tasks.Add(task);
                }
            }
            catch (Exception e) { Console.WriteLine($"Ошибка загрузки задач: {e.Message}"); }
            return tasks;
        }

        private static async Task SaveTasks(List<TaskItem> tasks, string user)
        {
            try
            {
                var lines = tasks.Select(t => t.ToString()).ToList();
                var otherUserTasks = await LoadOtherUserTasks(user);
                lines.AddRange(otherUserTasks);

                await File.WriteAllLinesAsync(TasksFile, lines);
            }
            catch (Exception e) { Console.WriteLine($"Ошибка сохранения задач: {e.Message}"); }
        }

        private static async Task<List<string>> LoadOtherUserTasks(string currentUser)
        {
            var otherUserTasks = new List<string>();
            if (File.Exists(TasksFile))
            {
                foreach (var line in await File.ReadAllLinesAsync(TasksFile))
                {
                    var task = TaskItem.FromString(line);
                    if (task.Пользователь != currentUser)
                        otherUserTasks.Add(line);
                }
            }
            return otherUserTasks;
        }

        private static async Task<bool> UserExists(string user)
        {
            try
            {
                if (!File.Exists(UsersFile)) return false;
                foreach (var line in await File.ReadAllLinesAsync(UsersFile))
                {
                    if (line.Split('|')[0] == user) return true;
                }
                return false;
            }
            catch (Exception e) { Console.WriteLine($"Ошибка проверки пользователя: {e.Message}"); return false; }
        }
    }
}