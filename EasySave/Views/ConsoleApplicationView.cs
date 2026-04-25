public class ConsoleApplicationView
{
    public ConsoleApplicationView()
    {
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

            RenderHelp(viewModel.TextService);
        }

        if (viewModel.ConfiguredJobNumber.HasValue && viewModel.IsConfigurationMessage)
        {
            if (viewModel.Messages.Count > 0)
            {
                Console.WriteLine();
            }

            RenderSingleJob(viewModel.AvailableJobs, viewModel.ConfiguredJobNumber.Value, viewModel.TextService);
        }
        else if (viewModel.ShowJobList)
        {
            if (viewModel.Messages.Count > 0 || viewModel.ShowHelp)
            {
                Console.WriteLine();
            }

            RenderJobList(viewModel.AvailableJobs, viewModel.TextService);
        }
    }

    private static void RenderHelp(ApplicationTextService textService)
    {
        foreach (string line in textService.GetHelpLines())
        {
            Console.WriteLine(line);
        }
    }

    private static void RenderJobList(IEnumerable<BackupJob> jobs, ApplicationTextService textService)
    {
        Console.WriteLine(textService.GetConfiguredJobsHeader());

        foreach ((BackupJob job, int index) in jobs.Select((job, index) => (job, index)))
        {
            Console.WriteLine(textService.GetJobSummaryLine(index + 1, job));
            Console.WriteLine(textService.GetJobSourceLine(job.Source));
            Console.WriteLine(textService.GetJobTargetLine(job.Target));
            Console.WriteLine(textService.GetJobTypeLine(job.Type));
            Console.WriteLine(textService.GetJobConfigurationStatusLine(job));
            Console.WriteLine();
        }
    }

    private static void RenderSingleJob(IEnumerable<BackupJob> jobs, int jobNumber, ApplicationTextService textService)
    {
        BackupJob? job = jobs.ElementAtOrDefault(jobNumber - 1);
        if (job == null)
        {
            return;
        }

        Console.WriteLine(textService.GetConfiguredJobsHeader());
        Console.WriteLine(textService.GetJobSummaryLine(jobNumber, job));
        Console.WriteLine(textService.GetJobSourceLine(job.Source));
        Console.WriteLine(textService.GetJobTargetLine(job.Target));
        Console.WriteLine(textService.GetJobTypeLine(job.Type));
        Console.WriteLine(textService.GetJobConfigurationStatusLine(job));
        Console.WriteLine();
    }
}
