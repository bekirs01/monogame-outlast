// Запуск приложения.
using System;
using System.Runtime.InteropServices;

try
{
    Console.WriteLine("Outlast2D: запуск… (если окно не появилось, смотрите текст ошибки ниже)");
    Console.Out.Flush();

    using var game = new Outlast2D.Game1();
    game.Run();
}
catch (DllNotFoundException ex)
{
    Console.Error.WriteLine("Не найдена нативная библиотека (SDL2 / OpenAL). Проверьте runtimes/osx/native в папке вывода.");
    Console.Error.WriteLine(ex);
    Environment.ExitCode = 1;
    if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        Console.Error.WriteLine("Подсказка: запустите из Терминала macOS (не из Cursor); в некоторых средах графическое окно не создаётся.");
}
catch (Exception ex)
{
    Console.Error.WriteLine("Не удалось запустить игру:");
    Console.Error.WriteLine(ex);
    Environment.ExitCode = 1;
}
