using System;
using plog;
using plog.Models;

namespace GameConsole;

[Serializable]
public class ConsoleLog
{
	public Log log;

	public Logger source;

	public UnscaledTimeSince timeSinceLogged;

	public bool expanded;

	public ConsoleLog(Log log, Logger source)
	{
		this.log = log;
		timeSinceLogged = 0f;
		this.source = source;
		if (log.Level == Level.Error && log.StackTrace != null)
		{
			expanded = true;
		}
	}
}
