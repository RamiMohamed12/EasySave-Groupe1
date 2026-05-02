namespace CryptoSoft;

public static class Program
{
    public static void Main(string[] args)
    {
        try
        {
            foreach (var arg in args)
            {
                Console.WriteLine(arg);
            }

            var fileManager = new FileManager(args[0], args[1]);
            int ElapsedTime = fileManager.TransformFile();
            Environment.Exit(ElapsedTime);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            Environment.Exit(-99);
        }
    }
}