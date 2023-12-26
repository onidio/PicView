﻿using PicView.Core.Config;
using PicView.Core.FileHandling;
using PicView.WPF.FileHandling;
using PicView.WPF.PicGallery;
using PicView.WPF.UILogic;
using PicView.WPF.Views.UserControls.Gallery;
using System.Diagnostics;
using System.IO;
using PicView.Core.Gallery;
using PicView.WPF.ImageHandling;

namespace PicView.WPF.ChangeImage;

internal static class FileUpdateNavigation
{
    internal static FileSystemWatcher? watcher;

    internal static void Initiate(string path)
    {
        watcher ??= new FileSystemWatcher();
        watcher.Path = path;
        watcher.EnableRaisingEvents = true;
        watcher.Filter = "*.*";
        watcher.IncludeSubdirectories = SettingsHelper.Settings.Sorting.IncludeSubDirectories;
        watcher.Created += async (_, e) => await OnFileAdded(e).ConfigureAwait(false);
        watcher.Deleted += async (_, e) => await OnFileDeleted(e).ConfigureAwait(false);
        watcher.Renamed += async (_, e) => await OnFileRenamed(e).ConfigureAwait(false);
    }

    private static async Task OnFileRenamed(RenamedEventArgs e)
    {
        if (e.FullPath.IsSupported() == false)
        {
            if (Navigation.Pics.Contains(e.OldFullPath))
            {
                Navigation.Pics.Remove(e.OldFullPath);
            }
            return;
        }
        var fileInfo = new FileInfo(e.FullPath);
        if (fileInfo.Exists == false) { return; }

        var index = Navigation.Pics.IndexOf(e.FullPath);
        if (index < 0) { return; }

        try
        {
            Navigation.Pics[Navigation.Pics.IndexOf(e.OldFullPath)] = e.FullPath;
        }
        catch (Exception exception)
        {
#if DEBUG
            Trace.WriteLine($"{nameof(OnFileRenamed)} exception:\n{exception.Message}");
#endif
        }
        await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(ChangeTitlebar.SetTitle.SetTitleString);
        if (PreLoader.Contains(index))
        {
            PreLoader.Remove(index);
            await PreLoader.AddAsync(index).ConfigureAwait(false);
        }
        if (UC.GetPicGallery is not null)
        {
            var thumbData = GalleryThumbInfo.GalleryThumbHolder.GetThumbData(index, null, fileInfo);
            await UC.GetPicGallery?.Dispatcher?.InvokeAsync(() =>
            {
                if (UC.GetPicGallery.Container.Children[index] is not PicGalleryItem item)
                {
                    return;
                }

                item.FileName = thumbData.FileName;
                item.ThumbFileDate.Text = thumbData.FileDate;
                item.ThumbFileLocation.Text = thumbData.FileLocation;
                item.ThumbFileSize.Text = thumbData.FileSize;
            });
        }
    }

    private static async Task OnFileDeleted(FileSystemEventArgs e)
    {
        if (e.FullPath.IsSupported() == false)
        {
            return;
        }

        var index = Navigation.Pics.IndexOf(e.FullPath);
        if (index < 0) { return; }
        if (Navigation.Pics.Remove(e.FullPath)) { return; }

        await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(ChangeTitlebar.SetTitle.SetTitleString);
        if (UC.GetPicGallery is not null)
        {
            while (GalleryLoad.IsLoading)
            {
                await Task.Delay(100);
            }
            await UC.GetPicGallery?.Dispatcher?.InvokeAsync(() =>
            {
                UC.GetPicGallery.Container.Children.RemoveAt(index);
            });
        }

        if (PreLoader.Contains(index))
        {
            PreLoader.Remove(index);
        }
    }

    private static async Task OnFileAdded(FileSystemEventArgs e)
    {
        if (e.FullPath.IsSupported() == false)
        {
            return;
        }
        var fileInfo = new FileInfo(e.FullPath);
        if (fileInfo.Exists == false) { return; }

        var newList = await Task.FromResult(FileLists.FileList(fileInfo)).ConfigureAwait(false);
        if (newList.Count == 0) { return; }
        if (newList.Count == Navigation.Pics.Count) { return; }
        Navigation.Pics = newList;

        await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(ChangeTitlebar.SetTitle.SetTitleString);
        var index = Navigation.Pics.IndexOf(e.FullPath);
        if (index < 0) { return; }

        if (UC.GetPicGallery is not null)
        {
            var bitmapSource = await Thumbnails.GetBitmapSourceThumbAsync(Navigation.Pics[Navigation.FolderIndex],
                (int)GalleryNavigation.PicGalleryItemSize, fileInfo).ConfigureAwait(false);
            var thumbData = GalleryThumbInfo.GalleryThumbHolder.GetThumbData(Navigation.FolderIndex, bitmapSource, fileInfo);
            await UC.GetPicGallery?.Dispatcher?.InvokeAsync(() =>
            {
                var item = new PicGalleryItem(bitmapSource, e.FullPath, false)
                {
                    FileName = thumbData.FileName,
                    ThumbFileDate =
                    {
                        Text = thumbData.FileDate
                    },
                    ThumbFileLocation =
                    {
                        Text = thumbData.FileLocation
                    },
                    ThumbFileSize =
                    {
                        Text = thumbData.FileSize
                    }
                };
                UC.GetPicGallery.Container.Children.Insert(index, item);
            });
        }
        var nextIndex = index + 1;
        if (index >= Navigation.Pics.Count)
        {
            nextIndex = 0;
        }
        var prevIndex = index - 1;
        if (prevIndex < 0)
        {
            prevIndex = Navigation.Pics.Count - 1;
        }
        if (PreLoader.Contains(index) || PreLoader.Contains(nextIndex) || PreLoader.Contains(prevIndex))
        {
            PreLoader.Clear();
            await PreLoader.PreLoadAsync(Navigation.FolderIndex, Navigation.Pics.Count).ConfigureAwait(false);
        }
    }
}