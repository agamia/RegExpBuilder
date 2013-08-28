public class RegExBuilder
	{
		#region Member variables
		private StringBuilder _literal;
		private bool _ignoreCase;
		private bool _multiLine;
		private HashSet<char> _specialCharactersInsideCharacterClass;
		private HashSet<char> _specialCharactersOutsideCharacterClass;
		private StringBuilder _escapedString;
		private int _min;
		private int _max;
		private string _of;
		private bool _ofAny;
		private int _ofGroup;
		private string _from;
		private string _notFrom;
		private string _like;
		private string _either;
		private bool _reluctant;
		private bool _capture;
		#endregion

		#region Constructor
		public RegExBuilder()
		{
			_literal = new StringBuilder();
			_specialCharactersInsideCharacterClass = new HashSet<char>(new char[] { '^', '-', ']' });
			_specialCharactersOutsideCharacterClass = new HashSet<char>(new char[] { '.', '^', '$', '*', '+', '?', '(', ')', '[', '{' });
			_escapedString = new StringBuilder();
			Clear();
		}
		#endregion

		#region Private Methods
		private void Clear()
		{
			_ignoreCase = false;
			_multiLine = false;
			_min = -1;
			_max = -1;
			_of = "";
			_ofAny = false;
			_ofGroup = -1;
			_from = "";
			_notFrom = "";
			_like = "";
			_either = "";
			_reluctant = false;
			_capture = false;
		}

		private void FlushState()
		{
			if (_of != "" || _ofAny || _ofGroup > 0 || _from != "" || _notFrom != "" || _like != "")
			{
				string captureLiteral = _capture ? "" : "?:";
				string quantityLiteral = QuantityLiteral;
				string characterLiteral = CharacterLiteral;
				string reluctantLiteral = _reluctant ? "?" : "";
				_literal.Append("(" + captureLiteral + "(?:" + characterLiteral + ")" + quantityLiteral + reluctantLiteral + ")");
				Clear();
			}
		}

		private string QuantityLiteral
		{
			get
			{
				if (_min != -1)
				{
					if (_max != -1)
					{
						return "{" + _min + "," + _max + "}";
					}
					return "{" + _min + ",}";
				}
				return "{0," + _max + "}";
			}
		}

		private string CharacterLiteral
		{
			get
			{
				if (_of != "")
				{
					return _of;
				}
				if (_ofAny)
				{
					return ".";
				}
				if (_ofGroup > 0)
				{
					return "\\" + _ofGroup;
				}
				if (_from != "")
				{
					return "[" + _from + "]";
				}
				if (_notFrom != "")
				{
					return "[^" + _notFrom + "]";
				}
				if (_like != "")
				{
					return _like;
				}
				return "";
			}
		}

		private string escapeInsideCharacterClass(string s)
		{
			return escapeSpecialCharacters(s, _specialCharactersInsideCharacterClass);
		}

		private string escapeOutsideCharacterClass(string s)
		{
			return escapeSpecialCharacters(s, _specialCharactersOutsideCharacterClass);
		}

		private string escapeSpecialCharacters(string s, HashSet<char> specialCharacters)
		{
			_escapedString = new StringBuilder();
			for (int i = 0; i < s.Length; i++)
			{
				char character = s[i];
				if (specialCharacters.Contains(character))
				{
					_escapedString.Append("\\" + character);
				}
				else
				{
					_escapedString.Append(character);
				}
			}
			return _escapedString.ToString();
		}
		#endregion

		#region Properties
		public Regex RegEx
		{
			get
			{
				RegexOptions opt = (this._ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None) |
					(this._multiLine ? RegexOptions.Multiline : RegexOptions.None);
				Regex regex = new System.Text.RegularExpressions.Regex(this.Literal, opt);
				return regex;
			}
		}

		public virtual string Literal
		{
			get
			{
				FlushState();
				return _literal.ToString();
			}
		}
		#endregion

		#region Public Methods
		public virtual RegExBuilder IgnoreCase()
		{
			_ignoreCase = true;
			return this;
		}

		public virtual RegExBuilder MultiLine()
		{
			_multiLine = true;
			return this;
		}

		public virtual RegExBuilder StartOfInput()
		{
			_literal.Append("(?:^)");
			return this;
		}

		public virtual RegExBuilder StartOfLine()
		{
			MultiLine();
			return StartOfInput();
		}

		public virtual RegExBuilder EndOfInput()
		{
			FlushState();
			_literal.Append("(?:$)");
			return this;
		}

		public virtual RegExBuilder EndOfLine()
		{
			MultiLine();
			return EndOfInput();
		}

		public virtual RegExBuilder Either(RegExBuilder r)
		{
			FlushState();
			_either = r.Literal;
			return this;
		}

		public virtual RegExBuilder Either(string s)
		{
			return this.Either((new RegExBuilder()).Exactly(1).Of(s));
		}

		public virtual RegExBuilder Or(RegExBuilder r)
		{
			string either = _either;
			string or = r.Literal;
			if (either == "")
			{
				_literal.Remove(_literal.Length - 1, 1);
				_literal.Append("|(?:" + or + "))");
			}
			else
			{
				_literal.Append("(?:(?:" + either + ")|(?:" + or + "))");
			}
			Clear();
			return this;
		}

		public virtual RegExBuilder Or(string s)
		{
			return this.Or((new RegExBuilder()).Exactly(1).Of(s));
		}

		public virtual RegExBuilder Exactly(int n)
		{
			FlushState();
			_min = n;
			_max = n;
			return this;
		}

		public virtual RegExBuilder Min(int n)
		{
			FlushState();
			_min = n;
			return this;
		}

		public virtual RegExBuilder Max(int n)
		{
			FlushState();
			_max = n;
			return this;
		}

		public virtual RegExBuilder Of(string s)
		{
			_of = escapeOutsideCharacterClass(s);
			return this;
		}

		public virtual RegExBuilder OfAny()
		{
			_ofAny = true;
			return this;
		}

		public virtual RegExBuilder OfGroup(int n)
		{
			_ofGroup = n;
			return this;
		}

		public virtual RegExBuilder From(char[] s)
		{
			_from = escapeInsideCharacterClass(new string(s));
			return this;
		}

		public virtual RegExBuilder From(string s)
		{
			_from = escapeInsideCharacterClass(s);
			return this;
		}

		public virtual RegExBuilder NotFrom(char[] s)
		{
			_notFrom = escapeInsideCharacterClass(new string(s));
			return this;
		}

		public virtual RegExBuilder NotFrom(string s)
		{
			_notFrom = escapeInsideCharacterClass(s);
			return this;
		}

		public virtual RegExBuilder Like(RegExBuilder r)
		{
			_like = r.Literal;
			return this;
		}

		public virtual RegExBuilder Reluctantly()
		{
			_reluctant = true;
			return this;
		}

		public virtual RegExBuilder Ahead(RegExBuilder r)
		{
			FlushState();
			_literal.Append("(?=" + r.Literal + ")");
			return this;
		}

		public virtual RegExBuilder NotAhead(RegExBuilder r)
		{
			FlushState();
			_literal.Append("(?!" + r.Literal + ")");
			return this;
		}

		public virtual RegExBuilder AsGroup()
		{
			_capture = true;
			return this;
		}

		public virtual RegExBuilder Then(string s)
		{
			return Exactly(1).Of(s);
		}

		public virtual RegExBuilder Some(char[] s)
		{
			return Min(1).From(s);
		}

		public virtual RegExBuilder MaybeSome(char[] s)
		{
			return Min(0).From(s);
		}

		public virtual RegExBuilder Maybe(string s)
		{
			return Max(1).Of(s);
		}

		public virtual RegExBuilder Anything()
		{
			return Min(1).OfAny();
		}

		public virtual RegExBuilder LineBreak()
		{
			return Either("\r\n").Or("\r").Or("\n");
		}

		public virtual RegExBuilder LineBreaks()
		{
			return Like((new RegExBuilder()).LineBreak());
		}

		public virtual RegExBuilder Whitespace()
		{
			if (_min == -1 && _max == -1)
			{
				return Exactly(1).Of("\\s");
			}
			_like = "\\s";
			return this;
		}

		public virtual RegExBuilder Tab()
		{
			return Exactly(1).Of("\t");
		}

		public virtual RegExBuilder Tabs()
		{
			return Like((new RegExBuilder()).Tab());
		}

		public virtual RegExBuilder Digit()
		{
			return Exactly(1).Of("\\d");
		}

		public virtual RegExBuilder Digits()
		{
			return Like((new RegExBuilder()).Digit());
		}

		public virtual RegExBuilder Letter()
		{
			Exactly(1);
			_from = "A-Za-z";
			return this;
		}

		public virtual RegExBuilder Letters()
		{
			_from = "A-Za-z";
			return this;
		}

		public virtual RegExBuilder LowerCaseLetter()
		{
			Exactly(1);
			_from = "a-z";
			return this;
		}

		public virtual RegExBuilder LowerCaseLetters()
		{
			_from = "a-z";
			return this;
		}

		public virtual RegExBuilder UpperCaseLetter()
		{
			Exactly(1);
			_from = "A-Z";
			return this;
		}

		public virtual RegExBuilder UpperCaseLetters()
		{
			_from = "A-Z";
			return this;
		}

		public virtual RegExBuilder Append(RegExBuilder r)
		{
			Exactly(1);
			_like = r.Literal;
			return this;
		}

		public virtual RegExBuilder Optional(RegExBuilder r)
		{
			Max(1);
			_like = r.Literal;
			return this;
		}
		#endregion
	}
