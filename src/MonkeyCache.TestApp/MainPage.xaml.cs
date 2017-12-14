using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace MonkeyCache.TestApp
{
	public partial class MainPage : ContentPage
	{
        class Monkey
        {
            public string Name { get; set; }
        }
        public MainPage()
        {
            InitializeComponent();


            ButtonLoad.Clicked += ButtonLoad_Clicked;
            ButtonSave.Clicked += ButtonSave_Clicked;

            Barrel.UniqueId = "com.refractored.monkeycachetest";

            var monkey = Barrel.Current.Get<Monkey>("monkey");
            if (monkey != null)
                EntryName.Text = monkey.Name;
            else
                EntryName.Text = "Sebastian";

            ButtonExpired.Clicked += ButtonExpired_Clicked;

        }

        private void ButtonExpired_Clicked(object sender, EventArgs e)
        {
            DisplayAlert("Is Expired?", Barrel.Current.IsExpired("monkey") ? "Yes" : "No", "OK");
        }

        private void ButtonSave_Clicked(object sender, EventArgs e)
        {
            if(string.IsNullOrWhiteSpace(EntryName.Text))
            {
                DisplayAlert("Info", "Please enter a name", "OK");
                return;
            }
            var monkey = new Monkey { Name =  EntryName.Text};
            Barrel.Current.Add<Monkey>("monkey", monkey, TimeSpan.FromDays(1));
            DisplayAlert(":)", "Saved!", "OK");
        }

        private void ButtonLoad_Clicked(object sender, EventArgs e)
        {
            var monkey = Barrel.Current.Get<Monkey>("monkey");
            if (monkey == null)
                DisplayAlert(":(", "No Monkey", "OK");
            else
                DisplayAlert(":)", monkey.Name, "OK");
        }
    }
}
