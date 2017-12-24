﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using BarrelSQL = MonkeyCache.SQLite.Barrel;
using BarrelFile = MonkeyCache.FileStore.Barrel;
using BarrelLite = MonkeyCache.LiteDB.Barrel;
using BarrelRealm = MonkeyCache.Realm.Barrel;

namespace MonkeyCache.TestApp
{
	public partial class MainPage : ContentPage
	{
        class Monkey
        {
            public string Name { get; set; }
        }
		IBarrel sql;
		IBarrel file;
		IBarrel lite;
		IBarrel realm;
        public MainPage()
        {
            InitializeComponent();


            ButtonLoad.Clicked += ButtonLoad_Clicked;
            ButtonSave.Clicked += ButtonSave_Clicked;

            BarrelLite.ApplicationId = "com.refractored.monkeycachetestlite";
			BarrelFile.ApplicationId = "com.refractored.monkeycachetestfile";
			BarrelSQL.ApplicationId = "com.refractored.monkeycachetestsql";
			BarrelRealm.ApplicationId = "com.refractored.monkeycachetestrealm";

			sql = BarrelSQL.Current;
			lite = BarrelLite.Current;
			file = BarrelFile.Current;


			ButtonExpired.Clicked += ButtonExpired_Clicked;

        }

        private void ButtonExpired_Clicked(object sender, EventArgs e)
		{
			

			DisplayAlert("Is Expired?", GetCurrent().IsExpired("monkey") ? "Yes" : "No", "OK");
		}

		private IBarrel GetCurrent()
		{
			IBarrel current = null;
			if (UseSQLite.IsToggled)
				current = sql;
			else if (UseFileStore.IsToggled)
				current = file;
			else if (UseLiteDB.IsToggled)
				current = lite;
			else if (UseRealm.IsToggled)
				current = realm;
			else
				current = sql;//fallback

			return current;
		}

		private void ButtonSave_Clicked(object sender, EventArgs e)
        {
            if(string.IsNullOrWhiteSpace(EntryName.Text))
            {
                DisplayAlert("Info", "Please enter a name", "OK");
                return;
            }
            var monkey = new Monkey { Name =  EntryName.Text};
			GetCurrent().Add<Monkey>("monkey", monkey, TimeSpan.FromDays(1));
            DisplayAlert(":)", "Saved!", "OK");
        }

        private void ButtonLoad_Clicked(object sender, EventArgs e)
        {
            var monkey = GetCurrent().Get<Monkey>("monkey");
            if (monkey == null)
                DisplayAlert(":(", "No Monkey", "OK");
            else
                DisplayAlert(":)", monkey.Name, "OK");
        }
    }
}
