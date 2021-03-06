﻿using CuttingEdge.Conditions;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace LogicBrokerAccess.Throttling
{
	public sealed class Throttler : IDisposable
	{
		/// <summary>
		/// Max requests per restore time interval
		/// </summary>
		public int MaxQuota { get; private set; }
		public int RemainingQuota
		{
			get { return _remainingQuota; }
		}
		private volatile int _remainingQuota;

		/// <summary>
		///	API limits (total per day)
		/// </summary>
		public int DayLimit { get; set; }
		/// <summary>
		///	API requests (per day) remaining 
		/// </summary>
		public int DayLimitRemaining { get; set; }

		private readonly int _quotaRestoreTimeInSeconds;
		private readonly Timer _timer;
		private bool _timerStarted = false;
		private object _lock = new object();

		public Throttler( ThrottlingOptions throttlingOptions )
		{
			this.MaxQuota = this._remainingQuota = throttlingOptions.MaxRequestsPerTimeInterval;
			this._quotaRestoreTimeInSeconds = throttlingOptions.TimeIntervalInSec;

			_timer = new Timer( RestoreQuota, null, Timeout.Infinite, _quotaRestoreTimeInSeconds * 1000 );
		}

		public async Task< TResult > ExecuteAsync< TResult >( Func< Task< TResult > > funcToThrottle )
		{
			lock ( _lock )
			{
				if ( !_timerStarted )
				{
					_timer.Change( _quotaRestoreTimeInSeconds * 1000, _quotaRestoreTimeInSeconds * 1000 );
					_timerStarted = true;
				}
			}

			while( true )
			{
				return await this.TryExecuteAsync( funcToThrottle ).ConfigureAwait( false );
			}
		}

		private async Task< TResult > TryExecuteAsync< TResult >( Func< Task< TResult > > funcToThrottle )
		{
			await this.WaitIfNeededAsync().ConfigureAwait( false );

			var result = await funcToThrottle().ConfigureAwait( false );

			return result;
		}

		private async Task WaitIfNeededAsync()
		{
			while ( true )
			{
				lock (_lock)
				{
					if (_remainingQuota > 0)
					{
						_remainingQuota--;
#if DEBUG
						Trace.WriteLine($"[{ DateTime.Now }] We have quota remains { _remainingQuota }. Continue work" );
#endif
						return;
					}
				}

#if DEBUG
				Trace.WriteLine($"[{ DateTime.Now }] Quota remain { _remainingQuota }. Waiting { _quotaRestoreTimeInSeconds } seconds to continue" );
#endif

				await Task.Delay( _quotaRestoreTimeInSeconds * 1000 ).ConfigureAwait( false );
			}
		}

		/// <summary>
		///	Releases quota that we have for each period of time
		/// </summary>
		/// <param name="state"></param>
		private void RestoreQuota( object state = null )
		{
			this._remainingQuota = this.MaxQuota;

			#if DEBUG
				Trace.WriteLine($"[{ DateTime.Now }] Restored { MaxQuota } quota" );
			#endif
		}

		#region IDisposable Support
		private bool disposedValue = false;

		void Dispose( bool disposing )
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					_timer.Dispose();
				}

				disposedValue = true;
			}
		}

		public void Dispose()
		{
			Dispose( true );
		}
		#endregion
	}

	public class ThrottlingOptions
	{
		public int MaxRequestsPerTimeInterval { get; private set; }
		public int TimeIntervalInSec { get; private set; }

		public ThrottlingOptions( int maxRequests, int timeIntervalInSec )
		{
			Condition.Requires( maxRequests, "maxRequests" ).IsGreaterOrEqual( 1 );
			Condition.Requires( timeIntervalInSec, "timeIntervalInSec" ).IsGreaterOrEqual( 1 );

			this.MaxRequestsPerTimeInterval = maxRequests;
			this.TimeIntervalInSec = timeIntervalInSec;
		}

		public static ThrottlingOptions LogicBrokerDefaultThrottlingOptions
		{
			get
			{
				return new ThrottlingOptions( 1, 3 );
			}
		}
	}
}