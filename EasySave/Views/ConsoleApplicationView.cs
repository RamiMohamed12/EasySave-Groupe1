public class ConsoleApplicationView
{
    public void Render(ApplicationViewModel viewModel)
    {
        foreach (string message in viewModel.Messages)
        {
            Console.WriteLine(message);
        }
    }
}
