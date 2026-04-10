// Program başlar, oyunu çalıştırır.
using System;
using System.Runtime.InteropServices;

try
{
    Console.WriteLine("Outlast2D başlıyor… (pencere açılmazsa aşağıdaki hata metnine bak)");
    Console.Out.Flush();

    using var game = new Outlast2D.Game1();
    game.Run();
}
catch (DllNotFoundException ex)
{
    Console.Error.WriteLine("Yerel kütüphane bulunamadı (SDL2 / OpenAL). Çıktı klasöründe runtimes/osx/native var mı kontrol et.");
    Console.Error.WriteLine(ex);
    Environment.ExitCode = 1;
    if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        Console.Error.WriteLine("İpucu: Uygulamayı macOS Terminal’den (Cursor dışında) çalıştırmayı dene; bazı ortamlarda grafik penceresi oluşmaz.");
}
catch (Exception ex)
{
    Console.Error.WriteLine("Oyun başlatılamadı:");
    Console.Error.WriteLine(ex);
    Environment.ExitCode = 1;
}
