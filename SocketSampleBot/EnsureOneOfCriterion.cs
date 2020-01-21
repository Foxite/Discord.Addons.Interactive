using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;

namespace SampleBot {
	public class EnsureOneOfCriterion : ICriterion<SocketMessage> {
		private IEnumerable<string> m_Values;

		public EnsureOneOfCriterion(IEnumerable<string> values) {
			m_Values = values.Select(val => val.ToLower());
		}

		public EnsureOneOfCriterion(params string[] values) {
			m_Values = values.Select(val => val.ToLower());
		}

		public Task<bool> JudgeAsync(SocketCommandContext sourceContext, SocketMessage parameter) => Task.FromResult(m_Values.Contains(parameter.Content.ToLower()));
	}
}
