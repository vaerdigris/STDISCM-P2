# Build/Compile Instructions
## Prerequisites
Have the following installed:
- [.NET SDK](https://dotnet.microsoft.com/en-us/download)
- [Visual Studio Code](https://code.visualstudio.com/)
- C# Dev Kit Extension for VS Code (available in the Extensions Marketplace)

## Steps to Build/Run
1. Download the project zip file, and extract to the desired location.
2. Open the project in VS Code. This can be done by:
    - clicking on **File > Open Folder** and selecting the extracted project folder, or
    - simply dragging the project folder into VS Code's explorer window.
3. Navigate to the **Terminal** tab and open a new terminal window. Once this is open, move to the inner project directory (if applicable) by running `cd <project-folder>` in the terminal.
4. Once this folder is open, you can choose to either build the project to create an executable file or run it directly in the terminal.

### Running the Program Directly in VS Code Terminal (Preferred)
1. Run the program by entering the command:
   ```
   dotnet run
   ```
   in the terminal.
2. The program will then display dungeon status updates and a final summary after execution.

### Building then Running the Executable File
1. Build the executable file by running the command:
   ```
   dotnet build
   ```
2. The executable file's location will be shown in the terminal. Usually, the compiled file is in:
   ```
   ./bin/Debug/netX.X/<project-name>.exe
   ```
3. Double-click on the `.exe` file to run the program.
4. Make sure to copy the config file **config.txt**, which is located in the project folder, to the same directory as the executable before running the program.

### Modifying Configuration File
1. The simulation parameters can be modified in **config.txt**. All values must be in `key=value` format with no whitespace. 
2. The required parameters are:
   - `n`: Maximum number of concurrent dungeon instances (e.g., `n=6`)
   - `t`: Number of tank players (e.g., `t=50`)
   - `h`: Number of healer players (e.g., `h=50`)
   - `d`: Number of DPS players (e.g., `d=100`)
   - `t1`: Minimum time (in seconds) a dungeon can take to clear (e.g., `t1=1`)
   - `t2`: Maximum time (in seconds) a dungeon can take to clear (e.g., `t2=4`)
3. The sample **config.txt** file is **located in the same folder as the source code**.

**Note: [Github Repository](https://github.com/vaerdigris/STDISCM-P2)**