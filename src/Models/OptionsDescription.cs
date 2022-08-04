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
        public bool? CollectStartup { get; set; }

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
