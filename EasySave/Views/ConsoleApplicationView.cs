public class ConsoleApplicationView
{
    private readonly ApplicationTextService _textService;

    public ConsoleApplicationView(ApplicationTextService textService)
    {
        _textService = textService;
    }

    public void Render(ApplicationViewModel viewModel)
    {
        foreach (string message in viewModel.Messages)
        {
            Console.WriteLine(message);
        }

        if (viewModel.ShowHelp)
        {
            if (viewModel.Messages.Count > 0)
            {
                Console.WriteLine();
            }

            RenderHelp();
        }

        if (viewModel.ShowJobList)
        {
            if (viewModel.Messages.Count > 0 || viewModel.ShowHelp)
            {
                Console.WriteLine();
            }

            RenderJobList(viewModel.AvailableJobs);
        }
    }

    private void RenderHelp()
    {
        foreach (string line in _textService.GetHelpLines())
        {
            Console.WriteLine(line);
        }
    }

    private void RenderJobList(IEnumerable<BackupJob> jobs)
    {
        Console.WriteLine(_textService.GetConfiguredJobsHeader());

        foreach ((BackupJob job, int index) in jobs.Select((job, index) => (job, index)))
        {
            Console.WriteLine(_textService.GetJobSummaryLine(index + 1, job));
            Console.WriteLine(_textService.GetJobSourceLine(job.Source));
            Console.WriteLine(_textService.GetJobTargetLine(job.Target));
            Console.WriteLine(_textService.GetJobTypeLine(job.Type));
            Console.WriteLine();
        }
    }
}
