# Recent Trading Data Console App

This is a simple .NET 8 Console application that uses the Alpaca Markets API and a PostgreSQL database to get and store minute-by-minute sale prices of a given stock.


# Database

The DB has two main uses:
<ul>
	<li>Store which ticker symbols to check for.</li>
	<li>Have a place to store the data each time it makes the calls</li>
</ul>

# Running

I have this application running on a Windows 2019 server with it as a scheduled task while market time is active. You can schedule this to run at different times throughout the day if you want only certain timeframes that is all configurable through task scheduler.
It uses a PG DB to store the data.
# MultiThreaded

This can make up to 200 requests a minute (limit of the free tier of AlpacaMarkets API). Because of that I run multiple multiple threads for efficiency otherwise this would be IO bound and very slow.

# Updates

<ul>
	<li>12/28/2023 - Initial Commit with basic functionality.</li>
	
<ul>

