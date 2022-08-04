// File: src/Models/Options.cs

// Class: OptionsDescription
public class OptionsDescription
{
    // Move this to a separate file when more Options descriptor classes get
    // added. Then, make Options a partial class.
    // Inner Class: TraceCollectDescription
    public class TraceCollectDescription
    {
        public string? Output { get; set; }

        private bool? _collectStartup;
        public string CollectStartup
        {
            get
            {
                return _collectStartup is null
                        ? "(null)"
                        : _collectStartup.ToString()!.ToLower();
            }

            set => _collectStartup = System.Convert.ToBoolean(value);
        }

        public override string ToString()
        {
            var strBuilder = new System.Text.StringBuilder();
            strBuilder.AppendFormat("Trace Output Name: {0}\n",
                                    Output ?? "(null)");
            strBuilder.AppendFormat("Will Collect Startup: {0}\n",
                                    CollectStartup is null ? "(null)" : CollectStartup);
            return strBuilder.ToString();
        }
    }

    // ********************************************************************* //

    public TraceCollectDescription TraceCollect { get; set; }

    public OptionsDescription()
    {
        TraceCollect = new TraceCollectDescription();
    }
}
