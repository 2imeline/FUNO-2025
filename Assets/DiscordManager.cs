using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DiscordManager : MonoBehaviour
{
    Discord.Discord discord;
    void Start()
    {
        discord = new Discord.Discord(1369137933973585920, (ulong)Discord.CreateFlags.NoRequireDiscord);
    }

    void OnDisable()
    {
        discord.Dispose();
        ChangeActivity();
    }

    public void ChangeActivity()
    {
        var activityManager = discord.GetActivityManager();
        var activity = new Discord.Activity
        {
            State = "Playing",
            Details = "Hello world!"
        };
        activityManager.UpdateActivity(activity, (res) => {
            Debug.Log("Activity Updated!");
        });
    }

    // Update is called once per frame
    void Update()
    {
        discord.RunCallbacks();
    }
}
