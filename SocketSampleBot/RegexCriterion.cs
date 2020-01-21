using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;

namespace SampleBot {
	public class RegexCriterion : ICriterion<SocketMessage> {
		private readonly Regex m_Regex;

		public RegexCriterion(Regex regex) {
			m_Regex = regex;
		}

		public Task<bool> JudgeAsync(SocketCommandContext sourceContext, SocketMessage parameter) {
			return Task.FromResult(m_Regex.IsMatch(parameter.Content));
		}
	}
}
