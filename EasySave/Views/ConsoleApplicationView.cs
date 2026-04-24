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
            if (viewModel.IsConfigurationMessage)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(message);
                Console.ResetColor();
            }
            else if (viewModel.IsBackupResultMessage && (message.Contains("Sauvegarde completee") || message.Contains("Backup completed successfully")))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(message);
                Console.ResetColor();
            }
            else
            {
                Console.WriteLine(message);
            }
        }

        if (viewModel.ShowHelp)
        {
            if (viewModel.Messages.Count > 0)
            {
                Console.WriteLine();
            }

            RenderHelp();
        }

        if (viewModel.ConfiguredJobNumber.HasValue && viewModel.IsConfigurationMessage)
        {
            if (viewModel.Messages.Count > 0)
            {
                Console.WriteLine();
            }

            RenderSingleJob(viewModel.AvailableJobs, viewModel.ConfiguredJobNumber.Value);
        }
        else if (viewModel.ShowJobList)
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
            Console.WriteLine(_textService.GetJobConfigurationStatusLine(job));
            Console.WriteLine();
        }
    }

    private void RenderSingleJob(IEnumerable<BackupJob> jobs, int jobNumber)
    {
        BackupJob? job = jobs.ElementAtOrDefault(jobNumber - 1);
        if (job == null)
        {
            return;
        }

        Console.WriteLine(_textService.GetConfiguredJobsHeader());
        Console.WriteLine(_textService.GetJobSummaryLine(jobNumber, job));
        Console.WriteLine(_textService.GetJobSourceLine(job.Source));
        Console.WriteLine(_textService.GetJobTargetLine(job.Target));
        Console.WriteLine(_textService.GetJobTypeLine(job.Type));
        Console.WriteLine(_textService.GetJobConfigurationStatusLine(job));
        Console.WriteLine();
    }
}
