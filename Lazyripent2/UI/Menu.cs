namespace Lazyripent2.UI;
/// <summary>
/// Menu.cs - Simple TUI for an even lazier way of using Lazyripent
/// - Outerbeast
/// </summary>
public static class Menu
{   // (ab)using Secondary bind for menu labels for now
    private static readonly BindOption[] MenuItems =
    [
        new( "0", "Exit",    "Close the application",               0, _ => { } ),
        new( "1", "Extract", "Extract entities from BSP files",     0, _ => RunExtractEntities() ),
        new( "2", "Import",  "Import entities into BSP files",      0, _ => RunImportEntities() ),
        new( "3", "Rule",    "Apply rule files to BSP/ENT/MAP files",0, _ => RunApplyRule() ),
        new( "4", "Strip Fgd","Strip FGD files",            0, _ => RunStripFgd() ),
    ];
    /// <summary>
    /// Runs the main menu loop, allowing the user to select and execute menu options until they choose to exit.
    /// </summary>
    /// <returns>bool: true if the user wants to continue, false if they want to exit.</returns>
    public static bool Display()
	{
        Console.WriteLine( "\nSelect an option:" );
        foreach( var opt in MenuItems )
            Console.WriteLine( $"[{opt.PrimaryBind}] {opt.SecondaryBind}\t{opt.Description}" );

        if( int.TryParse( GetKeyStroke().ToString(), out int value )
        && value >= (int) OperationMode.None
        && value <= (int) OperationMode.StripFgdOnly )
        {
            OperationMode choice = (OperationMode) value;
            // Elsewhere "Operation.None" would be default for "ApplyRules" but making an exception here for the menu.
            if( choice == OperationMode.None )
                return false;

            var selected = MenuItems.FirstOrDefault( item => item.PrimaryBind == value.ToString() );
            var prevColour = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine( $"\nSelected: {selected?.SecondaryBind}\n" );
            Console.ForegroundColor = prevColour;

            selected?.Action.Invoke( null );// Execute action for chosen option
        }
        else
        {
            var prevColour = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine( "Invalid choice. Please try again." );
            Console.ForegroundColor = prevColour;
        }

        return true;
	}
    /// <summary>
    /// Waits for the user to press a key and returns the character of the key that was pressed, 
    /// ignoring any control keys (e.g. Shift, Ctrl, etc.).
    /// </summary>
    /// <returns>char: key that was pressed.</returns>
    private static char GetKeyStroke()
    {
        while( true )
        {
            var keyInfo = Console.ReadKey( true );
            if( !char.IsControl( keyInfo.KeyChar ) )
                return keyInfo.KeyChar;
        }
    }
    /// <summary>
    /// Prompts the user to enter a file or folder entRemove, reads the input, and returns the full entRemove.
    /// </summary>
    /// <param name="prompt"></param>
    /// <returns>string: full entRemove entered by the user.</returns>
	private static string GetPath(string prompt)
	{
		Console.Write( prompt );
		var input = Console.ReadLine();

		if( string.IsNullOrWhiteSpace( input ) )
			return string.Empty;

		var path = input.Trim().Trim( '"' );

		if( string.IsNullOrWhiteSpace( path ) )
			return string.Empty;

		return Path.GetFullPath( path );
	}
    /// <summary>
    /// Runs the "Extract Entities" menu option, asking for a folder to run BSPs or a single BSP file
    /// </summary>
    private static void RunExtractEntities()
	{
		var path = GetPath( "Drag or enter a bsp file or bsp folder (leave blank to use current folder):\n>" );

        if( string.IsNullOrEmpty( path ) )
            return;

        Options.Reset();
        Options.SetMode( OperationMode.ExportEntOnly );

		if( !string.IsNullOrEmpty( path ) && File.Exists( path ) )// Single BSP
		{
            Options.AddInput( path );
            Options.AddOutput( Path.ChangeExtension( path, ".ent" ) );
		}
		else// Folder containing BSPs
		{
            Options.AddInput( string.IsNullOrEmpty( path ) ? Directory.GetCurrentDirectory() : path );
            Options.AddOutput( string.IsNullOrEmpty( path ) ? Directory.GetCurrentDirectory() : path );
		}

        Options.ValidateInputs();
        Options.MakeOutputs();
        Options.ValidateOutputs();

		Program.LoadFGD();
		Program.ExportEntsFromBspFiles();

        if( Options.OutputFileFullPaths.Count > 0 )
        {
            var prevColour = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;
            foreach( var bspFile in Options.OutputFileFullPaths )
                Console.WriteLine( $"Extracted: {bspFile}" );

            Console.ForegroundColor = prevColour;
        }
    }
    /// <summary>
    /// Runs the "Import Entities" menu option, prompting the user for a bsp file/folder and a corresponding ent file/folder,
    /// </summary>
    private static void RunImportEntities()
	{
		var path = GetPath( "Drag or enter a bsp file or bsp folder (leave blank to use current folder):\n>" );
        if( string.IsNullOrEmpty( path ) )
            return;

        Options.Reset();
        Options.SetMode( OperationMode.ImportEntOnly );
        // Track processed .ent files if any
        List<string> entFilesProcessed = [];

		if( !string.IsNullOrEmpty( path ) && File.Exists( path ) )
		{
            var entFile = Path.ChangeExtension( path, ".ent" );
            Options.AddInput( entFile );
            Options.AddOutput( path );// BSP file is the output
            Options.ValidateInputs();
            Options.MakeOutputs();
            Options.ValidateOutputs();
            entFilesProcessed.Add( entFile );
        }
		else
		{
            Options.AddInput( string.IsNullOrEmpty( path ) ? Directory.GetCurrentDirectory() : path );
            Options.ValidateInputs();
            Options.OutputFileFullPaths.Clear();

			foreach( var entFile in Options.GetFilesOfType( Options.InputFileFullPaths, FileType.Ent ) )
			{
				var bspPath = Path.ChangeExtension( entFile, ".bsp" );
				if( !File.Exists( bspPath ) )
				{
                    var prevColour = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine( $"Warning: matching .bsp not found for \"{entFile}\"" );
                    Console.ForegroundColor = prevColour;

                    continue;
				}

                Options.OutputFileFullPaths.Add( bspPath );
                entFilesProcessed.Add( entFile );
            }

			if( Options.OutputFileFullPaths.Count == 0 )
			{
                var prevColour = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine( "No valid .ent/.bsp pairs found." );
                Console.ForegroundColor = prevColour;

				return;
			}
		}

		Program.LoadFGD();
		Program.ImportEntsToBspFiles();

        if( Options.OutputFileFullPaths.Count > 0 )
        {
            var prevColour = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;
            foreach( var bspFile in Options.OutputFileFullPaths )
                Console.WriteLine( $"Imported: {bspFile}" );

            Console.ForegroundColor = prevColour;
        }
        else
        {
            var prevColour = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine( "No .ent files were imported." );
            Console.ForegroundColor = prevColour;

            return;
        }
        // Ask user for cleanup after import
        //Assert.Equal( Options.OutputFileFullPaths.Count, entFilesProcessed.Count );
        if( Options.PromptUser( "\nDo you want to delete imported .ent files?", 
            PromptAllowedOptions.YesNo, PromptOption.No ) )
        {
            foreach( var entRemove in entFilesProcessed )
            {
                try
                {
                    File.Delete( entRemove );
                    var prevColour = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine( $"Deleted: {entRemove}" );
                    Console.ForegroundColor = prevColour;
                }
                catch( Exception e )
                {
                    var prevColour = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine( $"Error deleting {entRemove}: {e.Message}" );
                    Console.ForegroundColor = prevColour;
                }
            }
        }
    }
    /// <summary>
    /// Runs the "Apply Rule" menu option, prompting the user for a rule file/folder and a bsp file/folder, 
	/// then applying the rules to the specified files.
    /// </summary>
    private static void RunApplyRule()
	{
		var rulePath = GetPath( "Drag or enter a rule file or rule folder (leave blank to use current folder):\n>" );
        if( string.IsNullOrEmpty( rulePath ) )
            return;

        Options.Reset();
        Options.SetMode( OperationMode.ApplyRuleFile );

		if( !string.IsNullOrEmpty( rulePath ) && File.Exists( rulePath ) )
            Options.AddInput( rulePath );
		else
            Options.AddInput( string.IsNullOrEmpty( rulePath ) ? Directory.GetCurrentDirectory() : rulePath );
        // BSP files
        var bspPath = GetPath( "Drag or enter a bsp file or bsp folder (leave blank to use current folder):\n>" );

        if( !string.IsNullOrEmpty( bspPath ) && File.Exists( bspPath ) )
		{
            Options.AddInput( bspPath );
            Options.AddOutput( bspPath );
		}
        else// Directories
        {
			var dir = string.IsNullOrEmpty( bspPath ) ? Directory.GetCurrentDirectory() : bspPath;
            Options.AddInput( dir );
            Options.AddOutput( dir );
		}

        Options.ValidateInputs();
        Options.MakeOutputs();
        Options.ValidateOutputs();

		Program.LoadFGD();
		Program.LoadRules();

        Program.ApplyRulesToFiles(
            Options.GetFilesOfType( Options.InputFileFullPaths, FileType.Map ),
            Options.GetFilesOfType( Options.OutputFileFullPaths, FileType.Map ) );

        Program.ApplyRulesToFiles(
            Options.GetFilesOfType( Options.InputFileFullPaths, FileType.Ent ),
            Options.GetFilesOfType( Options.OutputFileFullPaths, FileType.Ent ) );

        Program.ApplyRulesToFiles(
            Options.GetFilesOfType( Options.InputFileFullPaths, FileType.Bsp ),
            Options.GetFilesOfType( Options.OutputFileFullPaths, FileType.Bsp ) );
	}
    /// <summary>
    /// Runs the "Strip FGD" operation, prompting the user to select an FGD file.
    /// </summary>
    private static void RunStripFgd()
    {
        var fgdPath = GetPath( "Drag or enter an .fgd file:\n>" );
        if( string.IsNullOrEmpty( fgdPath ) )
            return;

        Options.Reset();
        Options.SetMode( OperationMode.StripFgdOnly );
        Options.SetFgdPath( fgdPath );

        var targetPath = GetPath( "Drag or enter a .map/.ent/.bsp file or folder (leave blank to use current folder):\n>" );

        if( !string.IsNullOrEmpty( targetPath ) && File.Exists( targetPath ) )
        {
            Options.AddInput( targetPath );
            Options.AddOutput( targetPath );
        }
        else
        {
            var dir = string.IsNullOrEmpty( targetPath ) ? Directory.GetCurrentDirectory() : targetPath;
            Options.AddInput( dir );
            Options.AddOutput( dir );
        }

        Options.ValidateInputs();
        Options.MakeOutputs();
        Options.ValidateOutputs();

        Program.LoadFGD();
        Program.StripFgdFromFiles(
            Options.GetFilesOfType( Options.InputFileFullPaths, FileType.Map ),
            Options.GetFilesOfType( Options.OutputFileFullPaths, FileType.Map ) );
        Program.StripFgdFromFiles(
            Options.GetFilesOfType( Options.InputFileFullPaths, FileType.Ent ),
            Options.GetFilesOfType( Options.OutputFileFullPaths, FileType.Ent ) );
        Program.StripFgdFromFiles(
            Options.GetFilesOfType( Options.InputFileFullPaths, FileType.Bsp ),
            Options.GetFilesOfType( Options.OutputFileFullPaths, FileType.Bsp ) );
    }
}
