﻿using CommunityToolkit.Mvvm.ComponentModel;
using HotLyric.Win32.Utils;
using HotLyric.Win32.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vanara.PInvoke;

namespace HotLyric.Win32.Models
{
    public class HotKeyManager : ObservableObject
    {
        private const string HotKeySettingKeyTemplate = "Settings_HotKey_";

        private readonly SettingsWindowViewModel settingViewModel;
        private HotKeyListener? hotKeyListener;
        private HotKeyModel[] hotKeyModels;
        private Dictionary<string, int> defaultHotKeys = new Dictionary<string, int>()
        {
            ["PlayPause"] = HotKeyModel.BuildSettingValue(User32.HotKeyModifiers.MOD_CONTROL | User32.HotKeyModifiers.MOD_ALT, User32.VK.VK_P),
            ["PrevMedia"] = HotKeyModel.BuildSettingValue(User32.HotKeyModifiers.MOD_CONTROL | User32.HotKeyModifiers.MOD_ALT, User32.VK.VK_LEFT),
            ["NextMedia"] = HotKeyModel.BuildSettingValue(User32.HotKeyModifiers.MOD_CONTROL | User32.HotKeyModifiers.MOD_ALT, User32.VK.VK_RIGHT),
            ["ShowHideLyric"] = HotKeyModel.BuildSettingValue(User32.HotKeyModifiers.MOD_CONTROL | User32.HotKeyModifiers.MOD_ALT, User32.VK.VK_D),
            ["LockUnlock"] = HotKeyModel.BuildSettingValue(User32.HotKeyModifiers.MOD_CONTROL | User32.HotKeyModifiers.MOD_ALT, User32.VK.VK_E),
            ["OpenPlayer"] = HotKeyModel.BuildSettingValue(User32.HotKeyModifiers.MOD_CONTROL | User32.HotKeyModifiers.MOD_ALT, User32.VK.VK_H),
        };

        public HotKeyManager(SettingsWindowViewModel settingViewModel)
        {
            PlayPauseKeyModel = new HotKeyModel("PlayPause");
            PrevMediaKeyModel = new HotKeyModel("PrevMedia");
            NextMediaKeyModel = new HotKeyModel("NextMedia");
            ShowHideLyricKeyModel = new HotKeyModel("ShowHideLyric");
            LockUnlockKeyModel = new HotKeyModel("LockUnlock");
            OpenPlayerKeyModel = new HotKeyModel("OpenPlayer");

            hotKeyModels = new[]
            {
                PlayPauseKeyModel,
                PrevMediaKeyModel,
                NextMediaKeyModel,
                ShowHideLyricKeyModel,
                LockUnlockKeyModel,
                OpenPlayerKeyModel,
            };

            foreach (var model in hotKeyModels)
            {
                var settingValue = settingViewModel.LoadSetting($"{HotKeySettingKeyTemplate}{model.HotKeyName}", defaultHotKeys[model.HotKeyName]);

                (model.Modifiers, model.Key) = HotKeyModel.GetKeyFromSettingValue(settingValue);

                model.PropertyChanged += KeyModel_PropertyChanged;
            }

            this.settingViewModel = settingViewModel;
        }

        public HotKeyModel PlayPauseKeyModel { get; }

        public HotKeyModel PrevMediaKeyModel { get; }

        public HotKeyModel NextMediaKeyModel { get; }

        public HotKeyModel ShowHideLyricKeyModel { get; }

        public HotKeyModel LockUnlockKeyModel { get; }

        public HotKeyModel OpenPlayerKeyModel { get; }


        private void KeyModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (sender is HotKeyModel model)
            {
                settingViewModel.SetSettings($"{HotKeySettingKeyTemplate}{model.HotKeyName}", model.ToSettingValue());

                Refresh();
            }
        }

        public void Install()
        {
            lock (this)
            {
                if (hotKeyListener == null)
                {
                    hotKeyListener = new HotKeyListener();
                    hotKeyListener.HotKeyInvoked += HotKeyListener_HotKeyInvoked;
                }
            }

            Refresh();
        }

        public void Uninstall()
        {
            if (hotKeyListener != null)
            {
                lock (this)
                {
                    hotKeyListener.HotKeyInvoked -= HotKeyListener_HotKeyInvoked;
                    hotKeyListener?.Dispose();
                    hotKeyListener = null;
                }
            }
        }

        private void HotKeyListener_HotKeyInvoked(HotKeyListener sender, HotKeyInvokedEventArgs args)
        {
            var model = hotKeyModels.FirstOrDefault(c => c.Key == args.Key && c.Modifiers == args.Modifier);

            if (model != null)
            {
                HotKeyInvoked?.Invoke(this, new HotKeyManagerHotKeyInvokedEventArgs(model));
            }
        }

        public event HotKeyManagerHotKeyInvokedEventHandler? HotKeyInvoked;

        public void ResetToDefaultSettings()
        {
            foreach (var model in hotKeyModels)
            {
                (model.Modifiers, model.Key) = HotKeyModel.GetKeyFromSettingValue(defaultHotKeys[model.HotKeyName]);
            }

            Refresh();
        }

        public void Refresh()
        {
            var hotKeyListener = this.hotKeyListener;
            if (hotKeyListener == null) return;

            try
            {
                hotKeyListener.UnregisterAllKeys().Wait();

                foreach (var item in hotKeyModels)
                {
                    if (HotKeyHelper.IsCompleted(item.Modifiers, item.Key))
                    {
                        item.IsEnabled = hotKeyListener.RegisterKey(item.Modifiers, item.Key).Result;
                    }
                    else
                    {
                        item.IsEnabled = true;
                    }
                }
            }
            catch { }

            OnPropertyChanged(nameof(PlayPauseKeyModel));
            OnPropertyChanged(nameof(PrevMediaKeyModel));
            OnPropertyChanged(nameof(NextMediaKeyModel));
            OnPropertyChanged(nameof(ShowHideLyricKeyModel));
            OnPropertyChanged(nameof(LockUnlockKeyModel));
            OnPropertyChanged(nameof(OpenPlayerKeyModel));
        }
    }

    public delegate void HotKeyManagerHotKeyInvokedEventHandler(HotKeyManager sender, HotKeyManagerHotKeyInvokedEventArgs args);

    public record HotKeyManagerHotKeyInvokedEventArgs(HotKeyModel HotKeyModel);
}
