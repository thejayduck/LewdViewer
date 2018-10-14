﻿using Android.App;
using Android.OS;
using Android.Support.V7.App;
using Android.Widget;
using System.IO;
using Android;
using Android.Support.V4.App;
using Android.Content.PM;
using Android.Graphics;
using System.Collections.Generic;
using Android.Support.Design.Widget;
using System.Threading.Tasks;
using Java.IO;
using Android.Views;
using System.Collections;
using Android.Gms.Ads;
using Plugin.Share;
using Plugin.CurrentActivity;
using System;
using System.Net;

namespace Lewd_Images
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme")]
    public class MainActivity : AppCompatActivity
    {
        //#FD4281 (253, 66, 129, 100) - pink button color
        //#424040 (66, 64, 64, 100) - faded out pink color

        //bools
        bool loading = false;
        bool downloading = false;

        //Buttons
        FloatingActionButton previousImageButton;

        //Tags
        ImageView imagePanel;
        Spinner tagSpinner;
        string ImageName => System.IO.Path.GetFileNameWithoutExtension(imageStore.GetLink());
        private static readonly string[] PERMISSIONS = { Manifest.Permission.WriteExternalStorage, Manifest.Permission.Internet , Manifest.Permission.AccessNetworkState};
        private static readonly int REQUEST_PERMISSION = 3;

        private string SelectedTag {
            get {
                if (tagSpinner.SelectedItemPosition >= 0)
                    return NekosLife.Instance.Tags[tagSpinner.SelectedItemPosition];
                else
                    return NekosLife.Instance.DefaultTag;
            }
        }

        public static LewdImageStore imageStore = new LewdImageStore(NekosLife.Instance);

        int ImagePanelOffscreenX => Resources.DisplayMetrics.WidthPixels;

        protected override void OnCreate(Bundle bundle) 
        {
            base.OnCreate(bundle);
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);

            CheckForPermissions();

            //Finding Resources
            tagSpinner = FindViewById<Spinner>(Resource.Id.tagSpinner);
            imagePanel = FindViewById<ImageView>(Resource.Id.imageView);
            previousImageButton = FindViewById<FloatingActionButton>(Resource.Id.previousImageButton);
            FloatingActionButton nextImageButton = FindViewById<FloatingActionButton>(Resource.Id.nextImageButton);
            AdView adView = FindViewById<AdView>(Resource.Id.adView);
            Android.Support.V7.Widget.Toolbar toolbar = FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.toolbar);

            //SetAdView
            MobileAds.Initialize(this, "ca-app-pub-5157629142822799~8600251110");
            var adRequest = new AdRequest.Builder().Build();
            adView.LoadAd(adRequest);

            //Toolbar Configurations
            SetSupportActionBar(toolbar);
            SupportActionBar.Title = "Nekos";

            tagSpinner.Adapter = new ArrayAdapter(this, Android.Resource.Layout.SimpleListItem1, new ArrayList(NekosLife.Instance.Tags));

            tagSpinner.ItemSelected += (o, e) =>
            {
                imageStore.Tag = SelectedTag;
                Toast.MakeText(this, $"Selected {SelectedTag}", ToastLength.Short).Show();
            };

            //Request image download vvv
            imagePanel.LongClick += (o, e) =>
            {
                if(imagePanel.Drawable == null)
                {
                    Toast.MakeText(this, "No Images Were Found!", ToastLength.Short).Show();
                    return;
                }

                Android.App.AlertDialog.Builder aDialog;
                aDialog = new Android.App.AlertDialog.Builder(this);
                aDialog.SetTitle("Image Options");
                aDialog.SetPositiveButton("Download Image", delegate 
                {
                    if (downloading)
                    {
                        Toast.MakeText(this, "An Image Is Being Downloaded Please Be Patient", ToastLength.Short).Show();
                        return;
                    }

                    downloading = true;
                    imagePanel.Animate().ScaleX(1.1f);
                    imagePanel.Animate().ScaleY(1.1f);
                    Task.Run(() =>
                    {
                        MemoryStream buffer = new MemoryStream();
                        imageStore.GetImage().Compress(Bitmap.CompressFormat.Png, 0, buffer);
                        buffer.Seek(0, SeekOrigin.Begin);
                        BufferedInputStream stream = new BufferedInputStream(buffer);
                        DownloadManager download = new DownloadManager(this, stream, buffer.Length);
                        download.Execute(ImageName + ".png");
                        RunOnUiThread(() =>
                        {
                            Toast.MakeText(this, $"Downloaded {ImageName}!", ToastLength.Short).Show();
                            imagePanel.Animate().ScaleX(1);
                            imagePanel.Animate().ScaleY(1);
                        });
                        downloading = false;
                    });
                });
                aDialog.SetNeutralButton("Set As Wallpaper", delegate 
                {
                    WallpaperManager.GetInstance(this).SetBitmap(imageStore.GetImage());
                    Toast.MakeText(this, "Wallpaper has been applied!", ToastLength.Short).Show();
                });
                aDialog.Show();
            };

            //Buttons Functions
            nextImageButton.LongClick += (o, e) =>
            {
                if (loading || downloading)
                {
                    Toast.MakeText(this, "An Image Is Being Downloaded or Loading Please Be Patient", ToastLength.Short).Show();
                    return;
                }
                Toast.MakeText(this, "Last image", ToastLength.Short).Show();
                loading = true;
                imagePanel.Animate().TranslationX(-ImagePanelOffscreenX);
                Task.Run(() =>
                {
                    imageStore.GotoLast();
                    Fix();
                    RunOnUiThread(() =>
                    {
                        ReloadImagePanel();
                        CheckPreviousImageButton();
                        imagePanel.TranslationX = ImagePanelOffscreenX;
                        imagePanel.Animate().TranslationX(0);
                    });
                    loading = false;
                });
            };
            nextImageButton.Click += (o,e) =>
            {
                GetNextImage();
            };
            previousImageButton.Click += (o, e) =>
            {
                GetPreviousImage();
            };

            Settings.Instance.OnLewdTagsEnabledChange += delegate
            {
                tagSpinner.Adapter = new ArrayAdapter(this, Android.Resource.Layout.SimpleListItem1, new ArrayList(NekosLife.Instance.Tags));
            };

            //Load image first time
            ReloadImagePanel();
        }

        /// <summary>
        /// Gets the next image and sets it to the image panel
        /// </summary>
        /// <param name="animation">if it should be animated(may not be animated if Settings.AnimationsEnabled is false)</param>
        public void GetNextImage(bool animate = true)
        {
            if (loading || downloading)
            {
                Toast.MakeText(this, "An Image Is Being Downloaded or Loading Please Be Patient", ToastLength.Short).Show();
                return;
            }

            Toast.MakeText(this, "Forward", ToastLength.Short).Show();
            loading = true;
            if (animate && Settings.Instance.AnimationsEnabled)
                imagePanel.Animate().TranslationX(-ImagePanelOffscreenX);

            Task.Run(() =>
            {
                try
                {
                    imageStore.Forward();
                    if (animate && Settings.Instance.AnimationsEnabled)
                        Fix();

                    RunOnUiThread(() =>
                    {
                        ReloadImagePanel();
                        previousImageButton.Visibility = ViewStates.Visible;

                        if (animate && Settings.Instance.AnimationsEnabled)
                        {
                            imagePanel.TranslationX = ImagePanelOffscreenX;
                            imagePanel.Animate().TranslationX(0);
                        }
                    });
                }
                catch (Exception e)
                {
                    RunOnUiThread(() =>
                    {
                        Toast.MakeText(this, e.ToString(), ToastLength.Long).Show();
                    });
                }
                finally
                {
                    loading = false;
                }
            });
        }

        /// <summary>
        /// Gets the previous image and sets it to the image panel
        /// </summary>
        /// <param name="animation">if it should be animated(may not be animated if Settings.AnimationsEnabled is false)</param>
        public void GetPreviousImage(bool animate = true)
        {
            if (loading || downloading)
            {
                Toast.MakeText(this, "An Image Is Being Downloaded or Loading Please Be Patient", ToastLength.Short).Show();
                return;
            }

            Toast.MakeText(this, "Backwards", ToastLength.Short).Show();
            loading = true;
            if(animate && Settings.Instance.AnimationsEnabled)
                imagePanel.Animate().TranslationX(ImagePanelOffscreenX);

            Task.Run(() =>
            {
                imageStore.Back();
                if (animate && Settings.Instance.AnimationsEnabled)
                    Fix();

                RunOnUiThread(() =>
                {
                    ReloadImagePanel();
                    CheckPreviousImageButton();

                    if (animate && Settings.Instance.AnimationsEnabled)
                    {
                        imagePanel.TranslationX = -ImagePanelOffscreenX;
                        imagePanel.Animate().TranslationX(0);
                    }
                });
                loading = false;
            });
        }

        public void CheckPreviousImageButton()
        {
            previousImageButton.Visibility = imageStore.IsFirst ? ViewStates.Invisible : ViewStates.Visible;
        }

        public void ReloadImagePanel()
        {
            imagePanel.SetImageBitmap(imageStore.GetImage());
            UpdateFavorite();
        }

        public void UpdateFavorite()
        {
            imagePanel.SetBackgroundColor(imageStore.IsCurrentFavorite ? Color.Gold : Color.Transparent);
        }

        public override void OnBackPressed()
        {
            Android.App.AlertDialog.Builder aDialog;
            aDialog = new Android.App.AlertDialog.Builder(this);
            aDialog.SetTitle("Are You Sure About Quitting?");
            aDialog.SetPositiveButton("YES", delegate { Process.KillProcess(Process.MyPid()); });
            aDialog.SetNegativeButton("NO", delegate { aDialog.Dispose(); });
            aDialog.Show();
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.toolbar_menu, menu);
            return base.OnCreateOptionsMenu(menu);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            Android.App.AlertDialog.Builder aDialog;
            aDialog = new Android.App.AlertDialog.Builder(this);

            Activity activity = CrossCurrentActivity.Current.Activity;
            View view = FindViewById(Android.Resource.Id.Content);

            if (item.ItemId == Resource.Id.menu_share)
            {
                if (!CrossShare.IsSupported || imagePanel.Drawable == null)
                {
                    return false;   
                }

                CrossShare.Current.Share(new Plugin.Share.Abstractions.ShareMessage
                {
                    Title = "Lewd Image",
                    Text = "Checkout this Neko!",
                    Url = imageStore.GetLink()
                });
            }
            if(item.ItemId == Resource.Id.menu_favorite)
            {
                if (imagePanel.Drawable == null)
                    return false;

                if (imageStore.IsCurrentFavorite)
                    imageStore.RemoveCurrentFromFavorite();
                else
                    imageStore.AddCurrentToFavorite();
                UpdateFavorite();

            }
            if (item.ItemId == Resource.Id.menu_info) 
            {
                aDialog.SetTitle("App Info")
                .SetMessage("Made By:" +
                "\n" +
                "Jay and Nobbele" +
                "\n" +
                "Images From:" +
                "\n" +
                "Nekos.life")
                .SetNeutralButton("OK", delegate { aDialog.Dispose(); })
                .Show();
            }
            if(item.ItemId == Resource.Id.menu_options)
            {
                LinearLayout layout = new LinearLayout(this)
                {
                    Orientation = Orientation.Vertical
                };
                layout.SetPadding(30, 20, 30, 20);

                Switch lewdSwitch = new Switch(this)
                {
                    Text = "Enable NSFW Tags",
                    Checked = Settings.Instance.LewdTagsEnabled
                };
                lewdSwitch.CheckedChange += delegate
                {
                    Settings.Instance.LewdTagsEnabled = lewdSwitch.Checked;
                };

                Switch animationSwitch = new Switch(this)
                {
                    Text = "Enable Animations",
                    Checked = Settings.Instance.AnimationsEnabled
                };
                animationSwitch.CheckedChange += delegate
                {
                    Settings.Instance.AnimationsEnabled = animationSwitch.Checked;
                };

                Button resetButton = new Button(this)
                {
                    Text = "Reset Image History"
                };
                resetButton.Click += (o, e) =>
                {
                    Snackbar.Make(view, "Cleared Image History", Snackbar.LengthShort).Show();
                    string link = imageStore.GetLink();
                    imageStore.Reset();
                    imageStore.AddLink(link);
                    previousImageButton.Visibility = ViewStates.Invisible;
                };

                Button serverCheckerButton = new Button(this)
                {
                    Text = "Check NekosLife Server"
                };
                serverCheckerButton.Click += delegate
                {
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://nekos.life/");
                    using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                        if (response == null || response.StatusCode != HttpStatusCode.OK)
                        {
                            Toast.MakeText(this, "Server Does Not Respond", ToastLength.Short).Show();
                            serverCheckerButton.SetTextColor(Color.Red);
                            serverCheckerButton.Text = "Error";
                        }
                        else
                        {
                            Toast.MakeText(this, "Server Works Fine", ToastLength.Short).Show();
                            serverCheckerButton.SetTextColor(Color.DarkGreen);
                            serverCheckerButton.Text = "Success";
                        }
                };

                layout.AddView(lewdSwitch);
                layout.AddView(animationSwitch);
                layout.AddView(resetButton);
                layout.AddView(serverCheckerButton);
                aDialog.SetView(layout)
                .SetTitle("Options")
                .SetNegativeButton("Help?", delegate
                {
                    aDialog.Dispose();
                    Snackbar.Make(view, "Click :Here: to learn more", Snackbar.LengthLong)
                    .SetAction("Click Here", V => HelpInfo()).Show();
                })
                .Show();
            }

            return base.OnOptionsItemSelected(item);
        }

        void HelpInfo()
        {
            Android.App.AlertDialog.Builder aDialog;
            aDialog = new Android.App.AlertDialog.Builder(this);

            LinearLayout layout = new LinearLayout(this)
            {
                Orientation = Orientation.Vertical
            };
            layout.SetPadding(30, 20, 30, 20);

            //Components
            TextView helpText = new TextView(this)
            {
                Text = $":Buttons And Their Functionality:" +
                $"\n" +
                $"Forward: Generates New Image \n " +
                $"(you can hold down on forward to go back to the last image)" +
                $"\n" +
                $"Backwards Goes Back One Image" +
                $"\n" +
                $"Dropdown: Choose The Tag You Want!" +
                $"\n" +
                $"Image: When A New Image Is Generated Hold Your Finger Down On It To See More Options!" +
                $"\n" +
                $"Share Image: Gives Sharing Options" +
                $"\n" +
                $"Favorite Button: Saves Your Favorited Images In A List To Use Them In App" +
                $"\n" +
                $"\n" +
                $":Options And Their Functionality:" +
                $"\n" +
                $"Enable NSFW Tags: Enables (lewd) Tags" +
                $"\n" +
                $"Enable Animations: Enables Animations (Saves Performance When Disabled)" +
                $"\n" +
                $"Reset Image History: Resets The Generated Image List (Saves Performance)" +
                $"\n" +
                $"Check NekosLife Server: To Check Is Host Is Online",
                Gravity = GravityFlags.CenterHorizontal
            };

            //Add Views
            layout.AddView(helpText);

            aDialog.SetView(layout)
            .SetTitle("Help")
            .SetNegativeButton("Close", delegate
            {
                aDialog.Dispose();
            })
            .Show();
        }

        private void CheckForPermissions()
        {
            if (ActivityCompat.CheckSelfPermission(this, Manifest.Permission.WriteExternalStorage) != (int)Permission.Granted || ActivityCompat.CheckSelfPermission(this, Manifest.Permission.Internet) != (int)Permission.Granted) 
            {
                ActivityCompat.RequestPermissions(this, PERMISSIONS, REQUEST_PERMISSION);
            }
        }

        //wtf, fixes animations for some reason
        public void Fix()
        {
            var _ = WebRequest.Create(NekosLife.APIUri + "neko").GetResponse();
        }
    }
}