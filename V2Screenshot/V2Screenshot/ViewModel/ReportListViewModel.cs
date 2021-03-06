﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace V2Screenshot.ViewModel
{
    class ReportListViewModel : WorkspaceViewModel
    {

        #region properties

        public override string Title
        {
            get
            {
                return "Reports";
            }
        }

        public ObservableCollection<ReportViewModel> Reports { get; private set; }

        #endregion //properties


        #region commands

        public Command CopyReportCommand { get; private set; }

        public Command RemoveReportCommand { get; private set; }

        public Command CopyReportMDCommand { get; private set; }
        

        public Command RemoveAllCommand { get; private set; }

        #endregion //commands

        #region constructor

        public ReportListViewModel()
        {
            Reports = new ObservableCollection<ReportViewModel>();

            CopyReportCommand = new Command(CmdCopyReport);
            CopyReportMDCommand = new Command(CmdCopyReportMD);
            RemoveReportCommand = new ParameterCommand<ReportViewModel>(CmdRemoveReport);
            RemoveAllCommand = new Command(CmdRemoveAll);
        }

        #endregion


        #region methods

        public void AddReport(ReportViewModel report)
        {
            if(!Reports.Contains(report))
            {
                Reports.Add(report);
            }
        }

        public void RemoveReport(ReportViewModel report)
        {
            Reports.Remove(report);
            if(!report.PlayerViewModel.IsOpen)
            {
                report.PlayerViewModel.Screenshot = null;
            }
        }

        #endregion


        #region command methods

        private void CmdCopyReport()
        {
            string text = String.Empty;

            foreach(ReportViewModel report in Reports)
            {
                string profileUrl = "https://fjql7u2zyeb4vwdk.onion.cab/memberlist.php?mode=viewprofile&u=" + report.PlayerViewModel.PlayerId;
                string name = report.PlayerViewModel.PlayerName;
                string server = report.PlayerViewModel.ServerHostname;
                string date = report.PlayerViewModel.ScreenshotDate.Value.ToString("yyyy-MM-dd HH:mm");
                string reason = report.ReportReason;

                string proof = String.Empty;
                foreach(string url in report.ImageUrls)
                {
                    proof += String.Format("[img]{0}.jpg[/img]\n", url);
                }

                text += String.Format(
                    "[b]Name:[/b] [url={0}]{1}[/url]\n" +
                    "[b]Server:[/b] {2}\n" +
                    "[b]Date:[/b] {3} UTC\n" +
                    "[b]Reason:[/b] {4}\n"+
                    "[b]Proof:[/b]\n[spoiler]{5}[/spoiler]\n",
                    profileUrl, name, server, date, reason, proof);
                
            }
            
            Clipboard.SetText(text);
        }

        private void CmdCopyReportMD()
        {
            string text = String.Empty;

            foreach (ReportViewModel report in Reports)
            {
                string profileUrl = "https://fjql7u2zyeb4vwdk.onion.cab/memberlist.php?mode=viewprofile&u=" + report.PlayerViewModel.PlayerId;
                string name = report.PlayerViewModel.PlayerName;
                string server = report.PlayerViewModel.ServerHostname;
                string date = report.PlayerViewModel.ScreenshotDate.Value.ToString("yyyy-MM-dd HH:mm");
                string reason = report.ReportReason;

                string proof = String.Empty;
                foreach (string url in report.ImageUrls)
                {
                    proof += String.Format("![Proof]({0}.jpg)\n", url);
                }

                text += String.Format(
                    "**Name:** [{1}]({0})\n" +
                    "**Server:** {2}\n" +
                    "**Date:** {3}\n" +
                    "**Reason:** {4}\n" +
                    "**Proof:**\n{5}\n",
                    profileUrl, name, server, date, reason, proof);

            }

            Clipboard.SetText(text);
        }

        private void CmdRemoveReport(ReportViewModel report)
        {
            RemoveReport(report);
        }

        private void CmdRemoveAll()
        {
            foreach(ReportViewModel report in Reports)
            {
                RemoveReport(report);
            }
        }

        #endregion //command methods

    }
}
