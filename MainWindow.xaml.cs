﻿using Microsoft.AspNetCore.SignalR.Client;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace Bit.VideoParty;

public partial class MainWindow
{
    const string vlcMediaUrl = "http://localhost:8080";
    const string serverUrl =
#if DEBUG
        "https://localhost:7036";
#else
        "https://bit-video-party.azurewebsites.net";
#endif
    const string vlcPassword = "P@ssw0rd";
    private HubConnection connection;

    public MainWindow()
    {
        InitializeComponent();

        Help.Text = @"Getting started:
1- Press `Ctrl + P` in VLC Player
2- In `Show Settings` choose `All`
3- Seach for `Lua` and select it in left pane
4- Set `Lua HTTP Password` as P@ssw0rd
5- Select `Main Interfaces` in left pane and check the `web` checkbox
6- Write group name down here and tap on Connect!";
    }

    private async void Connect_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Group.IsReadOnly = true;

            Connect.IsEnabled = false;

            if (connection is not null)
                await connection.DisposeAsync();

            if (string.IsNullOrWhiteSpace(Group.Text))
                throw new InvalidOperationException("Group name may not be empty!");

            connection = new HubConnectionBuilder()
                .WithUrl($"{serverUrl}/video-party-hub")
                .Build();

            connection.Closed += async (error) =>
            {
                Dispatcher.Invoke(() => Group.Foreground = Brushes.Red);
            };

            connection.On("Toggle", async () =>
            {
                await Dispatcher.InvokeAsync(DoToggle);
            });

            await connection.StartAsync();

            await connection.InvokeAsync("AddToGroup", Group.Text);

            Group.Foreground = Brushes.Green;
        }
        finally
        {
            Group.IsReadOnly = false;
            Connect.IsEnabled = true;
        }
    }

    private async void Toggle_Click(object sender, RoutedEventArgs e)
    {
        await DoToggle();

        using HttpClient client = new();
        (await client.PostAsync($"{serverUrl}/api/toggle?group={Group.Text}&senderConnectionId={connection.ConnectionId}", null)).EnsureSuccessStatusCode();
    }

    private async Task DoToggle()
    {
        try
        {
            Toggle.IsEnabled = false;

            using HttpClient client = new();
            byte[] password = Encoding.ASCII.GetBytes($":{vlcPassword}");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(password));
            (await client.GetAsync($"{vlcMediaUrl}/requests/status.xml?command=pl_pause")).EnsureSuccessStatusCode();
        }
        finally
        {
            Toggle.IsEnabled = true;
        }
    }
}