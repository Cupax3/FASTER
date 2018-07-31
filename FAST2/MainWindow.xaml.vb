﻿Imports System.ComponentModel
Imports System.Net
Imports System.Text.RegularExpressions
Imports System.Threading
Imports MaterialDesignColors
Imports MaterialDesignThemes.Wpf
Imports System.IO
Imports System.IO.Compression
Imports System.Text
Imports System.Windows.Forms
Imports System.Xml
Imports System.Xml.Serialization
Imports FAST2.Models

Class MainWindow
    Public InstallSteamCmd As Boolean = False
    Private ReadOnly _oProcess As New Process()
    Private _cancelled As Boolean

    Private Sub MainWindow_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        ISteamDirBox.Text = My.Settings.steamCMDPath
        ISteamUserBox.Text = My.Settings.steamUserName
        ISteamPassBox.Password = Encryption.DecryptData(My.Settings.steamPassword)
        IServerDirBox.Text = My.Settings.serverPath
        If InstallSteamCmd Then
            InstallSteam()
        End If
    End Sub

    'Takes any string and removes illegal characters
    Public Shared Function SafeName(input As String, Optional ignoreWhiteSpace As Boolean = False, Optional replacement As Char = "_") As String
        If ignoreWhiteSpace Then
            input = Regex.Replace(input, "[^a-zA-Z0-9\-_\s]", replacement)
            input = Replace(input, replacement & replacement, replacement)
            Return input
        Else
            input = Regex.Replace(input, "[^a-zA-Z0-9\-_]", replacement)
            input = Replace(input, replacement & replacement, replacement)
            Return input
        End If
    End Function

    'Creates new server profile in menu and tab control
    Private Sub CreateNewServerProfile(profileName As String)
        Dim safeProfileName As String = SafeName(profileName)

        Dim newServer As New ListBoxItem With {
            .Content = profileName,
            .Name = safeProfileName & "Select"
        }

        IServerProfilesList.Items.Add(newServer)

        AddHandler newServer.Selected, AddressOf MenuItemm_Selected

        Dim tabControls = New ServerProfileTab

        Dim newTab As New TabItem With {
            .Name = safeProfileName,
            .Content = tabControls
        }

        IMainContent.Items.Add(newTab)
        IMainContent.SelectedItem = newTab
    End Sub

    'Opens Folder select dialog and returns selected path
    Public Shared Function SelectFolder()
        Dim folderDialog As New FolderBrowserDialog

        If folderDialog.ShowDialog = vbOK Then
            Return folderDialog.SelectedPath
        Else
            Return Nothing
        End If
    End Function

    'Handles when any menu item is selected
    Private Sub MenuItemm_Selected(sender As ListBoxItem, e As RoutedEventArgs) Handles ISteamUpdaterTabSelect.Selected, ISteamModsTabSelect.Selected, ISettingsTabSelect.Selected, IToolsTabSelect.Selected, IAboutTabSelect.Selected
        Dim menus As New List(Of Controls.ListBox) From {
            IMainMenuItems,
            IServerProfilesList,
            IOtherMenuItems
        }

        For Each list In menus
            For Each item As ListBoxItem In list.Items
                If item.Name IsNot sender.Name Then
                    item.IsSelected = False
                End If
            Next
        Next

        For Each item As TabItem In IMainContent.Items
            If item.Name = sender.Name.Replace("Select", "") Then
                IMainContent.SelectedItem = item
            End If
        Next
    End Sub

    'Updates UI elements when window is resized/ re-rendered
    Private Sub MainWindow_LayoutUpdated(sender As Object, e As EventArgs) Handles Me.LayoutUpdated
        If Not WindowState = WindowState.Minimized Then
            'Sets max height of server profile containers to ensure no overflow to other menus
            IServerProfilesRow.MaxHeight = Height - 149
            IServerProfilesList.MaxHeight = IServerProfilesRow.ActualHeight - 50

            'Moves button to add new server profile as the menu expands
            Dim newMargin As Thickness = INewServerProfileButton.Margin
            newMargin.Left = IMenuColumn.ActualWidth - 130
            INewServerProfileButton.Margin = newMargin

            'Moves folder select buttons as the menu expands
            ISteamDirBox.Width = ISteamGroup.ActualWidth - 70
            IServerDirBox.Width = ISteamGroup.ActualWidth - 70
        End If
    End Sub

    'Creates a new Server Profile and adds it to the UI menu
    Private Sub NewServerProfileButton_Click(sender As Object, e As RoutedEventArgs) Handles INewServerProfileButton.Click
        Dim profileName As String = InputBox("Enter profile name:", "New Server Profile")

        If profileName = Nothing Then
            MsgBox("Please Enter A Value")
        Else
            CreateNewServerProfile(profileName)
        End If
    End Sub

    'Makes close button red when mouse is over button
    Private Sub WindowCloseButton_MouseEnter(sender As Object, e As Input.MouseEventArgs) Handles IWindowCloseButton.MouseEnter
        Dim converter = New BrushConverter()
        Dim brush = CType(converter.ConvertFromString("#D72C2C"), Brush)

        IWindowCloseButton.Background = brush
    End Sub

    'Changes colour of close button back to UI base when mouse leaves button
    Private Sub WindowCloseButton_MouseLeave(sender As Object, e As Input.MouseEventArgs) Handles IWindowCloseButton.MouseLeave
        Dim brush = FindResource("MaterialDesignPaper")

        IWindowCloseButton.Background = brush
    End Sub

    'Closes app when using custom close button
    Private Sub WindowCloseButton_Selected(sender As Object, e As RoutedEventArgs) Handles IWindowCloseButton.Selected
        Close()
    End Sub

    'Minimises app when using custom minimise button
    Private Sub WindowMinimizeButton_Selected(sender As Object, e As RoutedEventArgs) Handles IWindowMinimizeButton.Selected
        IWindowMinimizeButton.IsSelected = False
        WindowState = WindowState.Minimized
    End Sub

    'Allows user to move the window around using the custom nav bar
    Private Sub WindowDragBar_MouseDown(sender As Object, e As MouseButtonEventArgs) Handles IWindowDragBar.MouseLeftButtonDown, ILogoImage.MouseLeftButtonDown, IWindowTitle.MouseLeftButtonDown
        DragMove()
    End Sub

    'Opens folder select dialog when clicking certain buttons
    Private Sub DirButton_Click(sender As Object, e As RoutedEventArgs) Handles ISteamDirButton.Click, IServerDirButton.Click
        Dim path As String = SelectFolder()

        If path IsNot Nothing Then
            If sender Is ISteamDirButton Then
                ISteamDirBox.Text = path
            ElseIf sender Is IServerDirButton Then
                IServerDirBox.Text = path
            End If
        End If
    End Sub

    'Switches base theme between light and dark when control is switched 
    Private Sub IBaseThemeButton_Click(sender As Object, e As RoutedEventArgs) Handles IBaseThemeToggle.Click
        SwitchBaseTheme(IBaseThemeToggle.IsChecked)
        IWindowCloseButton.Background = FindResource("MaterialDesignPaper")
    End Sub

    'Turns toggle button groups into normal buttons
    Private Sub IActionButtons_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles IModActionButtons.SelectionChanged
        
        If IAddSteamMod.IsSelected
            Dim importDialog As New ImportSteamMod
            importDialog.Show()
        ElseIf IAddLocalMod.IsSelected
            AddLocalMod()
        End If
        
        Dim thread As New Thread(
            Sub()
                Thread.Sleep(600)
                Dispatcher.Invoke(
                    Sub()
                        sender.SelectedItem = Nothing
                    End Sub
                    )
            End Sub
            )
        thread.Start()
    End Sub

    Private Shared Sub AddLocalMod()
        Dim path As String = SelectFolder()
        Dim duplicate = False
        Dim currentMods As New ModCollection
        Dim modName = path.Substring(path.LastIndexOf("@", StringComparison.Ordinal) + 1)


        If My.Settings.mods IsNot Nothing
            currentMods = My.Settings.mods
        End If

        If currentMods.LocalMods.Count > 0 Then
            For Each localMod In currentMods.LocalMods
                If localMod.Name = modName
                    duplicate = true
                End If
            Next
        End If
       
        If Not duplicate Then
            currentMods.LocalMods.Add(New LocalMod(modName, path))

            My.Settings.mods = currentMods
        Else
            MsgBox("Mod already imported.")
        End If
    End Sub

    Public Shared Sub AddSteamMod(modUrl As String)

        If modUrl  Like "http*://steamcommunity.com/*/filedetails/?id=*" Then
            Dim duplicate = False
            Dim currentMods As New ModCollection

            If My.Settings.mods IsNot Nothing
                 currentMods = My.Settings.mods
            End If

            Dim modId = modUrl.Substring(modUrl.IndexOf("?id=", StringComparison.Ordinal))
            modId = Integer.Parse(Regex.Replace(modId, "[^\d]", ""))

            If currentMods.SteamMods.Count > 0 Then
                For Each steamMod In currentMods.SteamMods
                    If steamMod.WorkshopId = modId
                        duplicate = true
                    End If
                Next
            End If

            If Not duplicate Then
                Try
                    Dim modName As String
                    Dim appName As String
                    Dim sourceString As String

                    sourceString = New Net.WebClient().DownloadString(modUrl)

                    appName = sourceString.Substring(sourceString.IndexOf("content=" & ControlChars.Quote & "Steam Workshop:", StringComparison.Ordinal) + 25, 6)
                    
                    If appName Like "Arma 3" Then
                        
                        modName = sourceString.Substring(sourceString.IndexOf("<title>Steam Workshop :: ", StringComparison.Ordinal) + 25)
                        modName = StrReverse(modName)
                        modName = modName.Substring(modName.IndexOf(">eltit/<", StringComparison.Ordinal) + 8)
                        modName = StrReverse(modName)
                        modName = MainWindow.SafeName(modName)

                        Dim modInfo = MainWindow.GetModInfo(modId)

                        Dim steamUpdateTime = modInfo.Substring(modInfo.IndexOf("""time_updated"":") + 15, 10)

                        currentMods.SteamMods.Add(New SteamMod(modId, modName, "Unknown", steamUpdateTime, 0))

                        My.Settings.mods = currentMods
                    Else
                        Windows.MessageBox.Show("This is a workshop Item for a different game.")
                    End If
                Catch ex As Exception
                    MsgBox("An exception occurred:" & vbCrLf & ex.Message)
                End Try
            Else
                MsgBox("Mod already imported.")
            End If

        Else 
            Windows.MessageBox.Show("Please use format: https://steamcommunity.com/sharedfiles/filedetails/?id=*********")
        End If
        

    End Sub

    Public Shared Function GetModsFromXml(filename As String) As ModCollection
        Dim xml = File.ReadAllText(filename)
        Return Deserialize (Of ModCollection)(xml)
    End Function
    
    'Switches base theme between light and dark
    Private Shared Sub SwitchBaseTheme(isDark)
        Call New PaletteHelper().SetLightDark(isDark)
        My.Settings.isDark = isDark
    End Sub

    'Changes palette primary colour
    Private Shared Sub ApplyPrimary(swatch As Swatch)
        Call New PaletteHelper().ReplacePrimaryColor(swatch)
    End Sub

    'Changes palette accent colour
    Private Shared Sub ApplyAccent(swatch As Swatch)
        Call New PaletteHelper().ReplaceAccentColor(swatch)
    End Sub

    Private Sub InstallSteam()
        Windows.MessageBox.Show("Steam CMD will now download and start the install process. If prompted please enter your Steam Guard Code." & Environment.NewLine & "You will recieve this by email from steam. When this is all complete type 'quit' to finish.", "Information")

        Const url = "https://steamcdn-a.akamaihd.net/client/installer/steamcmd.zip"
        Dim fileName As String = My.Settings.steamCMDPath & "\steamcmd.zip"

        Dim client As New WebClient()

        AddHandler client.DownloadFileCompleted, New AsyncCompletedEventHandler(AddressOf SteamDownloadCompleted)
        client.DownloadFileAsync(New Uri(url), fileName)

        ISteamOutputBox.AppendText("Installing SteamCMD")
        ISteamOutputBox.AppendText(Environment.NewLine & "File Downloading...")
    End Sub

    Private Sub SteamDownloadCompleted(sender As Object, e As AsyncCompletedEventArgs)
        ISteamOutputBox.AppendText(Environment.NewLine & "Download Finished")

        Dim steamPath = My.Settings.steamCMDPath
        Dim zip As String = steamPath & "\steamcmd.zip"

        ISteamOutputBox.AppendText(Environment.NewLine & "Unzipping...")
        ZipFile.ExtractToDirectory(zip, steamPath)


        ISteamOutputBox.AppendText(Environment.NewLine & "Installing...")
        File.Delete(zip)

        RunSteamCommand(steamPath & "\steamcmd.exe", "+login anonymous +quit", "install")
    End Sub

    Private Async Sub RunSteamCommand(steamCmd As String, steamCommand As String, type As String, Optional modIDs As IReadOnlyCollection(Of String) = Nothing)
        If ReadyToUpdate() Then
            ISteamProgressBar.Value = 0
            ISteamCancelButton.IsEnabled = True
            IMainContent.SelectedItem = ISteamUpdaterTab
            Dim tasks As New List(Of Task)
            'updateServerButton.Enabled = False
            'modsTab.Enabled = False

            ISteamProgressBar.IsIndeterminate = True

            If type Is "addon" Then
                ISteamOutputBox.AppendText("Starting SteamCMD to update Addon" & Environment.NewLine & Environment.NewLine)
            ElseIf type Is "server" Then
                ISteamOutputBox.AppendText("Starting SteamCMD to update Server" & Environment.NewLine)
            End If

            tasks.Add(Task.Run(
                    Sub()
                        Dim oStartInfo As New ProcessStartInfo(steamCmd, steamCommand) With {
                            .CreateNoWindow = True,
                            .WindowStyle = ProcessWindowStyle.Hidden,
                            .UseShellExecute = False,
                            .RedirectStandardOutput = True,
                            .RedirectStandardInput = True,
                            .RedirectStandardError = True
                        }
                        _oProcess.StartInfo = oStartInfo
                        _oProcess.Start()

                        Dim sOutput As String

                        Dim oStreamReader As StreamReader = _oProcess.StandardOutput
                        Dim oStreamWriter As StreamWriter = _oProcess.StandardInput
                        Do
                            sOutput = oStreamReader.ReadLine

                            Dispatcher.Invoke(
                                    Sub()
                                        ISteamOutputBox.AppendText(Environment.NewLine & sOutput)
                                    End Sub
                                )

                            If sOutput Like "*at the console." Then
                                Dim steamCode As String

                                steamCode = InputBox("Enter Steam Guard code from email or mobile app.", "Steam Guard Code", "")
                                oStreamWriter.Write(steamCode & Environment.NewLine)
                            ElseIf sOutput Like "*Mobile Authenticator*" Then
                                Dim steamCode As String

                                steamCode = InputBox("Enter Steam Guard code from email or mobile app.", "Steam Guard Code", "")
                                oStreamWriter.Write(steamCode & Environment.NewLine)
                            End If

                            If sOutput Like "*Update state*" Then
                                Dim counter As Integer = sOutput.IndexOf(":", StringComparison.Ordinal)
                                Dim progress As String = sOutput.Substring(counter + 2, 2)
                                Dim progressValue As Integer

                                If progress.Contains(".") Then
                                    progressValue = progress.Substring(0, 1)
                                Else
                                    progressValue = progress
                                End If
                                Dispatcher.Invoke(
                                            Sub()
                                                ISteamProgressBar.IsIndeterminate = False
                                                ISteamProgressBar.Value = progressValue
                                            End Sub
                                    )
                            End If

                            If sOutput Like "*Success*" Then
                                Dispatcher.Invoke(
                                            Sub()
                                                ISteamProgressBar.Value = 100
                                            End Sub
                                    )
                            End If

                            If sOutput Like "*Timeout*" Then
                                MsgBox("Steam download timed out, please update mod again.")
                            End If

                            Dispatcher.Invoke(
                                Sub()
                                    If sOutput = Nothing Then

                                    Else
                                        ISteamOutputBox.AppendText(sOutput & Environment.NewLine)
                                    End If

                                    'IsteamOutputBox.SelectionStart = steamOutputBox.Text.Length
                                    ISteamOutputBox.ScrollToEnd()
                                End Sub
                                )

                        Loop While _oProcess.HasExited = False


                    End Sub
                ))

            Await Task.WhenAll(tasks)

            If (_cancelled = True) Then
                _cancelled = Nothing

                ISteamProgressBar.IsIndeterminate = False
                ISteamProgressBar.Value = 0

                ISteamOutputBox.Document.Blocks.Clear()
                ISteamOutputBox.AppendText("Process Cancelled")
            Else
                ISteamOutputBox.AppendText(Environment.NewLine & "Task Completed" & Environment.NewLine)
                ISteamOutputBox.ScrollToEnd()
                ISteamProgressBar.IsIndeterminate = False
                ISteamProgressBar.Value = 100

                If type Is "addon" Then
                    'UpdateModGrid()

                    'For Each item In modIDs
                    '    CopyKeys(item)
                    'Next

                ElseIf type Is "server" Then
                    MsgBox("Server Installed/ Updated.")
                ElseIf type Is "install" Then
                    MsgBox("SteamCMD Installed.")
                End If
            End If

            ISteamCancelButton.IsEnabled = False
            'modsTab.Enabled = True
            'updateServerButton.Enabled = True
            'modsDataGrid.PerformLayout()

        Else
            Windows.MessageBox.Show("Please check that SteamCMD is installed and that all fields are correct:" & Environment.NewLine & Environment.NewLine & "   -  Steam Dir" & Environment.NewLine & "   -  User Name & Pass" & Environment.NewLine & "   -  Server Dir", "Error")
        End If
    End Sub

    Private Function ReadyToUpdate() As Boolean
        If ISteamDirBox.Text = String.Empty Then
            Return False
        ElseIf ISteamUserBox.Text = String.Empty Then
            Return False
        ElseIf ISteamPassBox.Password = String.Empty Then
            Return False
        ElseIf IServerDirBox.Text = String.Empty Then
            Return False
        ElseIf (Not File.Exists(My.Settings.steamCMDPath & "\steamcmd.exe")) Then
            Return False
        Else
            Return True
        End If
    End Function

    Private Shared Sub ISteamCancelButton_Click(sender As Object, e As RoutedEventArgs) Handles ISteamUpdateButton.Click
        
    End Sub

    Private Shared Function Serialize(Of T)(value As T) As String
        If value Is Nothing Then
            Return Nothing
        End If

        Dim serializer = New XmlSerializer(GetType(T))
        Dim settings = New XmlWriterSettings() With {
                .Encoding = New UnicodeEncoding(False, False),
                .Indent = True,
                .OmitXmlDeclaration = False
                }

        Using textWriter As StringWriter = New StringWriter()

            Using xmlWriter As XmlWriter = XmlWriter.Create(textWriter, settings)
                serializer.Serialize(xmlWriter, value)
            End Using

            Return textWriter.ToString()
        End Using
    End Function

    Private Shared Function Deserialize(Of T)(xml As String) As T
        If String.IsNullOrEmpty(xml) Then
            Return Nothing
        End If

        Dim serializer = New XmlSerializer(GetType(T))
        Dim settings = New XmlReaderSettings()

        Using textReader = New StringReader(xml)

            Using xmlReader As XmlReader = XmlReader.Create(textReader, settings)
                Return serializer.Deserialize(xmlReader)
            End Using
        End Using
    End Function

    Public Shared Function GetModInfo(modId As String)
        Try
            ' Create a request using a URL that can receive a post.   
            Dim request As WebRequest = WebRequest.Create("https://api.steampowered.com/ISteamRemoteStorage/GetPublishedFileDetails/v1/")
            ' Set the Method property of the request to POST.  
            request.Method = "POST"
            ' Create POST data and convert it to a byte array.  
            Dim postData As String = "itemcount=1&publishedfileids[0]=" & modId
            Dim byteArray As Byte() = Encoding.UTF8.GetBytes(postData)
            ' Set the ContentType property of the WebRequest.  
            request.ContentType = "application/x-www-form-urlencoded"
            ' Set the ContentLength property of the WebRequest.  
            request.ContentLength = byteArray.Length
            ' Get the request stream.  
            Dim dataStream As Stream = request.GetRequestStream()
            ' Write the data to the request stream.  
            dataStream.Write(byteArray, 0, byteArray.Length)
            ' Close the Stream object.  
            dataStream.Close()
            ' Get the response.
            Dim response As WebResponse = Nothing
            Try
                response = request.GetResponse()
            Catch ex As Exception
                MsgBox("There may be an issue with Steam please try again shortly.")
            End Try
            ' Display the status. 
            Dim staus As String = CType(response, HttpWebResponse).StatusDescription
            ' Get the stream containing content returned by the server.  
            dataStream = response.GetResponseStream()
            ' Open the stream using a StreamReader for easy access.  
            Dim reader As New StreamReader(dataStream)
            ' Read the content.  
            Dim responseFromServer As String = reader.ReadToEnd()
            ' Clean up the streams.  
            reader.Close()
            dataStream.Close()
            response.Close()
            ' Return the content.  
            Return responseFromServer
            
        Catch ex As Exception
            MsgBox("GetModInfo - An exception occurred:" & vbCrLf & ex.Message)
            Return Nothing
        End Try
    End Function

    Private Sub MainWindow_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        My.Settings.Save()
    End Sub
End Class