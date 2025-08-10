# Recent Trading Data Console App

This is a simple .NET 8 Console application that uses the Alpaca Markets API and a PostgreSQL database to get and store minute-by-minute, day-by-day sale prices of a given stock. This also stores the summary of a stocks performance at the end of each trading day.


# Database

The DB has two main uses:
- Store which ticker symbols to check for.
- Have a place to store the data each time it makes the calls


# Running

I have this application running on a Windows 2019 server with it as a scheduled task while market time is active. You can schedule this to run at different times throughout the day if you want only certain timeframes that is all configurable through task scheduler.
It uses a PG DB to store the data.

# MultiThreaded

This can make up to 200 requests a minute (limit of the free tier of AlpacaMarkets API). Because of that I concatenate requests and run multiple threads for efficiency otherwise this would be IO bound and very slow.

# Updates

- 08.09.2025 1.1.0 - Calculating Daily Summary data after the trading day. Plans to later use this with ML.
- 12.28.2023 1.0.0 - Initial Commit with basic functionality.

