using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Audio;
using NAudio.Wave;
using System.Diagnostics;
using System.Security.Cryptography;

class Program
{
    private ulong serverId = 123;
    private string token = "";
    private DiscordSocketClient _client;
    private Dictionary<int, string> filesDict = new Dictionary<int, string>();
    private IVoiceChannel voiceChannel = null;

    public static async Task Main(string[] args)
    {
        var program = new Program();
        await program.MainAsync();
    }

    public async Task MainAsync()
    {
        _client = new DiscordSocketClient();

        // _client.Log += Log;

        await _client.LoginAsync(TokenType.Bot, token);
        await _client.StartAsync();

        await CommandLineInterfaceAsync();
    }

    private async Task CommandLineInterfaceAsync()
    {
        while(true){
            Console.Clear();
            Console.WriteLine("===== Menu =====");
            Console.WriteLine("1. Usar sons");
            Console.WriteLine("2. Selecionar canal");
            Console.WriteLine("3. Atualizar lista de sons");
            Console.WriteLine("4. Sair");
            Console.Write("Escolha uma opção: ");
            
            string option = Console.ReadLine() ?? string.Empty;

            switch (option)
            {
                case "1":
                    await UseSound();
                    break;
                case "2": 
                    try
                    {             
                        var guild = _client.GetGuild(serverId);
                        
                        var channels = guild?.VoiceChannels.Select(x => x.Name).ToArray();
                        for(int i = 0; i < channels.Count(); i++ ){
                            System.Console.WriteLine($"{i+1} - {channels[i]}");
                        }
                        
                        Console.WriteLine("Informe o canal de voz: ");
                        var x = int.Parse(Console.ReadLine());
                        voiceChannel = guild?.VoiceChannels.FirstOrDefault(vc => vc.Name == channels[x-1]);

                        UpdateSoundList();
                    }
                    catch{}
                    break;
                case "3":
                    UpdateSoundList();
                    break;
                case "4":
                    await _client.StopAsync();
                    return;
                default:
                    Console.WriteLine("Opção inválida! Tente novamente.");
                    break;
            }
        }
    }

    private void ShowSoundList(){
        Console.Clear();
        foreach (var item in filesDict)
        {
            System.Console.WriteLine($"{item.Key} {item.Value}");
        }
    }

    private async Task UseSound(){
        try
        {
            var audioClient = await voiceChannel.ConnectAsync();

            while (true)
            {
                ShowSoundList();
                Console.WriteLine("Digite '!{numero}' ou '!!' para sair.");
                var input = Console.ReadLine();
                
                if (input?.ToLower() == "!!")
                {
                    await voiceChannel.DisconnectAsync();
                    return;
                }

                if (input.StartsWith("!"))
                {
                    try
                    {
                        var audioFilePath = filesDict[int.Parse(input.Substring(1).Trim())];

                        if (voiceChannel == null)
                        {
                            Console.WriteLine("Canal de voz não encontrado.");
                            return;
                        }

                        await PlayAudioFileAsync(voiceChannel, audioFilePath, audioClient);
                    }
                    catch{}
                }
            }
        }
        catch{}
    }

    private void UpdateSoundList(){
        try
        {
            string folderPath = @"sounds/";
            string[] files = Directory.GetFiles(folderPath);

            for (int i = 0; i < files.Length; i++)
            {
                filesDict.Add(i + 1, files[i]);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao listar arquivos: {ex.Message}");
        }
    
    }

    private async Task PlayAudioFileAsync(IVoiceChannel voiceChannel, string filePath, IAudioClient audioClient)
    {
        using (var ffmpeg = CreateStream(filePath))
        using (var output = ffmpeg.StandardOutput.BaseStream)
        using (var discord = audioClient.CreatePCMStream(AudioApplication.Mixed))
        {
            try
            {
                await output.CopyToAsync(discord);
            }
            finally
            {
                await discord.FlushAsync();
            }
        }
    }

    private Process CreateStream(string path)
    {
        return Process.Start(new ProcessStartInfo
        {
            FileName = "ffmpeg",
            Arguments = $"-hide_banner -loglevel panic -i \"{path}\" -ac 2 -f s16le -ar 48000 pipe:1",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            CreateNoWindow = true
        });
    }

    private Task Log(LogMessage msg)
    {
        Console.WriteLine(msg.ToString());
        return Task.CompletedTask;
    }
}
