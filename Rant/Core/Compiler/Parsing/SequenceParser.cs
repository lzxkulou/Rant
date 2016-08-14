﻿using System;
using System.Collections.Generic;

using Rant.Core.Compiler.Syntax;
using Rant.Core.Stringes;

namespace Rant.Core.Compiler.Parsing
{
	internal class SequenceParser : Parser
	{
		public override IEnumerator<Parser> Parse(RantCompiler compiler, CompileContext context, TokenReader reader, Action<RantAction> actionCallback)
		{
			Token<R> token;

			while (!reader.End)
			{
				token = reader.ReadToken();

				switch (token.ID)
				{
					case R.LeftAngle:
						yield return Get<QueryParser>();
						break;

					case R.LeftSquare:
						yield return Get<TagParser>();
						break;

					case R.LeftCurly:
						reader.SkipSpace();
						yield return Get<BlockParser>();
						break;

					case R.Pipe:
						if (context == CompileContext.BlockSequence)
						{
							yield break;
						}
						else
						{
							goto default; // Print it if we're not in a block
						}

					case R.RightCurly:
						if (context == CompileContext.BlockSequence)
						{
							compiler.LeaveContext();
							yield break;
						}
						else
						{
							compiler.SyntaxError(token, "Unexpected block terminator");
						}
						break;

					// end of argument
					case R.Semicolon:
						if(context == CompileContext.ArgumentSequence)
						{
							yield break;
						}
						// this is probably just a semicolon in text
						actionCallback(new RAText(token));
						break;

					case R.RightSquare:
						// end of arguments
						if(context == CompileContext.ArgumentSequence || context == CompileContext.SubroutineBody)
						{
							compiler.LeaveContext();
							yield break;
						}
						compiler.SyntaxError(token, "Unexpected tag end", false);
						break;

					case R.RightAngle:
						compiler.SyntaxError(token, "Unexpected query end.");
						break;

					// the end of a block weight, maybe
					case R.RightParen:
						if(context == CompileContext.BlockWeight)
						{
							compiler.LeaveContext();
							yield break;
						}
						actionCallback(new RAText(token));
						break;

					case R.Whitespace:
						switch (context)
						{
							case CompileContext.BlockSequence:
								switch (reader.PeekType())
								{
									case R.Pipe:
									case R.RightCurly:
										continue; // Ignore whitespace at the end of block elements
								}
								break;
							default:
								actionCallback(new RAText(token));
								break;
						}
						break;
						
					case R.EscapeSequence: // Handle escape sequences
						actionCallback(new RAEscape(token));
						break;

					case R.EOF:
						if(context != CompileContext.DefaultSequence)
						{
							compiler.SyntaxError(token, "unexpected end-of-pattern");
						}
						yield break;

					default: // Handle text
						actionCallback(new RAText(token));
						break;
				}
			}

			if(reader.End && context != CompileContext.DefaultSequence)
			{
				compiler.SyntaxError(reader.PrevToken, "unexpected end-of-pattern");
			}
		}
	}
}