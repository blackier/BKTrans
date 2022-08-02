﻿#region License Information (GPL v3)

/*
    ShareX - A program that allows you to take screenshots and share any file type
    Copyright (c) 2007-2022 ShareX Team

    This program is free software; you can redistribute it and/or
    modify it under the terms of the GNU General Public License
    as published by the Free Software Foundation; either version 2
    of the License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program; if not, write to the Free Software
    Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.

    Optionally you can also view the license at <http://www.gnu.org/licenses/>.
*/

#endregion License Information (GPL v3)

using ShareX.HelpersLib;
using System;
using System.Windows.Forms;

namespace ShareX.MediaLib
{
    public static class FFmpegGitHubDownloader
    {
        public static DialogResult DownloadFFmpeg(bool async, DownloaderForm.DownloaderInstallEventHandler installRequested)
        {
            FFmpegUpdateChecker updateChecker = new FFmpegUpdateChecker("ShareX", "FFmpeg");
            string url = updateChecker.GetLatestDownloadURL(true);

            using (DownloaderForm form = new DownloaderForm(url, "ffmpeg.zip"))
            {
                form.Proxy = HelpersOptions.CurrentProxy.GetWebProxy();
                form.InstallType = InstallType.Event;
                form.RunInstallerInBackground = async;
                form.InstallRequested += installRequested;
                return form.ShowDialog();
            }
        }

        public static bool ExtractFFmpeg(string archivePath, string extractPath)
        {
            try
            {
                ZipManager.Extract(archivePath, extractPath, false, entry => entry.Name.Equals("ffmpeg.exe", StringComparison.OrdinalIgnoreCase), 100_000_000);
                return true;
            }
            catch (Exception e)
            {
                DebugHelper.WriteException(e);
            }

            return false;
        }
    }
}