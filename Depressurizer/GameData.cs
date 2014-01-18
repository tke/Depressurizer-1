﻿/*
Copyright 2011, 2012, 2013 Steve Labbe.

This file is part of Depressurizer.

Depressurizer is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

Depressurizer is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with Depressurizer.  If not, see <http://www.gnu.org/licenses/>.
*/
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Xml;
using Rallion;

namespace Depressurizer {
    /// <summary>
    /// Represents a single game
    /// </summary>
    public class Game {
        public string Name;
        public int Id;
        public Category Category;
        public bool Favorite;

        public Game( int id, string name ) {
            Id = id;
            Name = name;
            Category = null;
            Favorite = false;
        }
    }

    /// <summary>
    /// Represents a single game category
    /// </summary>
    public class Category : IComparable {
        public string Name;

        public Category( string name ) {
            Name = name;
        }

        public override string ToString() {
            return Name;
        }

        public int CompareTo( object o ) {
            return Name.CompareTo( ( o as Category ).Name );
        }
    }

    /// <summary>
    /// Represents a complete collection of games and categories.
    /// </summary>
    public class GameData {
        #region Fields
        public Dictionary<int, Game> Games;
        public List<Category> Categories;
        #endregion

        public GameData() {
            Games = new Dictionary<int, Game>();
            Categories = new List<Category>();
        }

        #region Modifiers
        public void Clear() {
            Games.Clear();
            Categories.Clear();
        }

        /// <summary>
        /// Sets the name of the given game ID, and adds the game to the list if it doesn't already exist.
        /// </summary>
        /// <param name="id">ID of the game to set</param>
        /// <param name="name">Name to assign to the game</param>
        /// <returns>True if game was not already in the list, false otherwise</returns>
        private bool SetGameName( int id, string name, bool overWrite ) {
            if( !Games.ContainsKey( id ) ) {
                Games.Add( id, new Game( id, name ) );
                return true;
            }
            if( overWrite ) {
                Games[id].Name = name;
            }
            return false;
        }

        /// <summary>
        /// Adds a new category to the list.
        /// </summary>
        /// <param name="name">Name of the category to add</param>
        /// <returns>The added category. Returns null if the category already exists.</returns>
        public Category AddCategory( string name ) {
            if( CategoryExists( name ) ) {
                return null;
            } else {
                Category newCat = new Category( name );
                Categories.Add( newCat );
                return newCat;
            }
        }

        /// <summary>
        /// Removes the given category.
        /// </summary>
        /// <param name="c">Category to remove.</param>
        /// <returns>True if removal was successful, false if it was not in the list anyway</returns>
        public bool RemoveCategory( Category c ) {
            if( Categories.Remove( c ) ) {
                foreach( Game g in Games.Values ) {
                    if( g.Category == c ) g.Category = null;
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Renames the given category.
        /// </summary>
        /// <param name="c">Category to rename.</param>
        /// <param name="newName">Name to assign to the new category.</param>
        /// <returns>True if rename was successful, false otherwise (if name was in use already)</returns>
        public bool RenameCategory( Category c, string newName ) {
            if( !CategoryExists( newName ) ) {
                c.Name = newName;
                Categories.Sort();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Sets the categories for the given list of game IDs to the same thing
        /// </summary>
        /// <param name="gameIDs">Array of game IDs.</param>
        /// <param name="newCat">Category to assign</param>
        public void SetGameCategories( int[] gameIDs, Category newCat ) {
            for( int i = 0; i < gameIDs.Length; i++ ) {
                Games[gameIDs[i]].Category = newCat;
            }
        }

        /// <summary>
        /// Sets the fav state for the given list of game IDs to the same thing
        /// </summary>
        /// <param name="gameIDs">Array of game IDs.</param>
        /// <param name="newCat">Fav state to assign</param>
        public void SetGameFavorites( int[] gameIDs, bool fav ) {
            for( int i = 0; i < gameIDs.Length; i++ ) {
                Games[gameIDs[i]].Favorite = fav;
            }
        }

        public int RemoveEmptyCategories() {
            Dictionary<Category, int> counts = new Dictionary<Category, int>();
            foreach( Category c in Categories ) {
                counts.Add( c, 0 );
            }
            foreach( Game g in Games.Values ) {
                if( g.Category != null && counts.ContainsKey( g.Category ) ) {
                    counts[g.Category] = counts[g.Category] + 1;
                }
            }
            int removed = 0;
            foreach( KeyValuePair<Category, int> pair in counts ) {
                if( pair.Value == 0 ) {
                    if( Categories.Remove( pair.Key ) ) {
                        removed++;
                    }
                }
            }
            return removed;
        }
        #endregion

        #region Accessors
        /// <summary>
        /// Checks to see if a category with the given name exists
        /// </summary>
        /// <param name="name">Name of the category to look for</param>
        /// <returns>True if the name is found, false otherwise</returns>
        public bool CategoryExists( string name ) {
            foreach( Category c in Categories ) {
                if( c.Name == name ) {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Gets the category with the given name. If the category does not exist, creates it.
        /// </summary>
        /// <param name="name">Name to get the category for</param>
        /// <returns>A category with the given name.</returns>
        public Category GetCategory( string name ) {
            if( string.IsNullOrEmpty( name ) ) return null;
            foreach( Category c in Categories ) {
                if( c.Name == name ) return c;
            }
            Category newCat = new Category( name );
            Categories.Add( newCat );
            return newCat;
        }
        #endregion
        public static XmlDocument FetchXmlGameList( string customUrl ) {
            return FetchXmlFromUrl( string.Format( Properties.Resources.UrlCustomGameListXml, customUrl ) );
        }

        public static XmlDocument FetchXmlGameList( Int64 steamId ) {
            return FetchXmlFromUrl( string.Format( Properties.Resources.UrlGameListXml, steamId ) );
        }

        public static XmlDocument FetchXmlFromUrl( string url ) {
            XmlDocument doc = new XmlDocument();
            try {
                Program.Logger.Write(LoggerLevel.Info, GlobalStrings.GameData_AttemptingDownloadXMLGameList, url);
                WebRequest req = HttpWebRequest.Create( url );
                WebResponse response = req.GetResponse();
                if( response.ResponseUri.Segments.Length < 4 ) {
                    throw new ProfileAccessException(GlobalStrings.GameData_SpecifiedProfileNotPublic);
                }
                doc.Load( response.GetResponseStream() );
                response.Close();
                Program.Logger.Write(LoggerLevel.Info, GlobalStrings.GameData_SuccessDownloadXMLGameList, url);
                return doc;
            } catch( ProfileAccessException e ) {
                Program.Logger.Write(LoggerLevel.Error, GlobalStrings.GameData_ProfileNotPublic);
                throw e;
            } catch( Exception e ) {
                Program.Logger.Write(LoggerLevel.Error, GlobalStrings.GameData_ExceptionDownloadXMLGameList, e.Message);
                throw new ApplicationException( e.Message, e );
            }
        }

        public static string FetchHtmlGameList( string customUrl ) {
            return FetchHtmlFromUrl( string.Format( Properties.Resources.UrlCustomGameListHtml, customUrl ) );
        }
        public static string FetchHtmlGameList( Int64 accountId ) {
            return FetchHtmlFromUrl( string.Format( Properties.Resources.UrlGameListHtml, accountId ) );
        }

        public static string FetchHtmlFromUrl( string url ) {
            try {
                string result = "";

                Program.Logger.Write(LoggerLevel.Info, GlobalStrings.GameData_AttemptingDownloadHTMLGameList, url);
                WebRequest req = HttpWebRequest.Create( url );
                using( WebResponse response = req.GetResponse() ) {
                    if( response.ResponseUri.Segments.Length < 4 ) {
                        throw new ProfileAccessException(GlobalStrings.GameData_SpecifiedProfileNotPublic);
                    }
                    StreamReader sr = new StreamReader( response.GetResponseStream() );
                    result = sr.ReadToEnd();
                }
                Program.Logger.Write(LoggerLevel.Info, GlobalStrings.GameData_SuccessDownloadHTMLGameList, url);
                return result;
            } catch( ProfileAccessException e ) {
                Program.Logger.Write(LoggerLevel.Error, GlobalStrings.GameData_ProfileNotPublic);
                throw e;
            } catch( Exception e ) {
                Program.Logger.Write(LoggerLevel.Error, GlobalStrings.GameData_ExceptionDownloadHTMLGameList, e.Message);
                throw new ApplicationException( e.Message, e );
            }
        }

        /// <summary>
        /// Integrates list of games from an XmlDocument into the loaded game list.
        /// </summary>
        /// <param name="doc">The XmlDocument containing the new game list</param>
        /// <param name="overWrite">If true, overwrite the names of games already in the list.</param>
        /// <param name="ignore">A set of item IDs to ignore.</param>
        /// <param name="ignoreDlc">Ignore any items classified as DLC in the database.</param>
        /// <returns>Returns the number of games successfully processed and not ignored.</returns>
        public int IntegrateXmlGameList( XmlDocument doc, bool overWrite, SortedSet<int> ignore, bool ignoreDlc, out int newItems ) {
            newItems = 0;
            if( doc == null ) return 0;
            int loadedGames = 0;
            XmlNodeList gameNodes = doc.SelectNodes( "/gamesList/games/game" );
            foreach( XmlNode gameNode in gameNodes ) {
                int appId;
                XmlNode appIdNode = gameNode["appID"];
                if( appIdNode != null && int.TryParse( appIdNode.InnerText, out appId ) ) {
                    XmlNode nameNode = gameNode["name"];
                    if( nameNode != null ) {
                        bool isNew;
                        bool added = IntegrateGame( appId, nameNode.InnerText, overWrite, ignore, ignoreDlc, out isNew );
                        if( added ) {
                            loadedGames++;
                            if( isNew ) {
                                newItems++;
                            }
                        }
                    }
                }
            }
            Program.Logger.Write(LoggerLevel.Info, GlobalStrings.GameData_IntegratedXMLDataIntoGameList, loadedGames, newItems);
            return loadedGames;
        }

        public int IntegrateHtmlGameList( string page, bool overWrite, SortedSet<int> ignore, bool ignoreDlc, out int newItems ) {
            newItems = 0;
            int totalItems = 0;

            Regex srch = new Regex( "\"appid\":([0-9]+),\"name\":\"([^\"]+)\"" );
            MatchCollection matches = srch.Matches( page );
            foreach( Match m in matches ) {
                if( m.Groups.Count < 3 ) continue;
                string appIdString = m.Groups[1].Value;
                string appName = m.Groups[2].Value;

                int appId;
                if( appName != null && appIdString != null && int.TryParse( appIdString, out appId ) ) {
                    appName = ProcessUnicode( appName );
                    bool isNew;
                    bool added = IntegrateGame( appId, appName, overWrite, ignore, ignoreDlc, out isNew );
                    if( added ) {
                        totalItems++;
                        if( isNew ) {
                            newItems++;
                        }
                    }
                }
            }
            Program.Logger.Write(LoggerLevel.Info, GlobalStrings.GameData_IntegratedHTMLDataIntoGameList, totalItems, newItems);
            return totalItems;
        }

        private static Regex rxUnicode = new Regex( @"\\u(?<Value>[a-zA-Z0-9]{4})", RegexOptions.Compiled );

        public string ProcessUnicode( string val ) {
            return rxUnicode.Replace(
                val,
                m => ( (char)int.Parse( m.Groups["Value"].Value, NumberStyles.HexNumber ) ).ToString()
            );
        }

        public bool IntegrateGame( int appId, string appName, bool overWrite, SortedSet<int> ignore, bool ignoreDlc, out bool isNew ) {
            isNew = false;
            if( ( ignore != null && ignore.Contains( appId ) ) || ( ignoreDlc && Program.GameDB.IsDlc( appId ) ) ) {
                Program.Logger.Write(LoggerLevel.Verbose, GlobalStrings.GameData_SkippedIntegratingGame, appId, appName);
                return false;
            }
            isNew = SetGameName( appId, appName, overWrite );
            Program.Logger.Write(LoggerLevel.Verbose, GlobalStrings.GameData_IntegratedGameIntoGameList, appId, appName, isNew);
            return true;
        }

        /// <summary>
        /// Loads category info from the given steam config file.
        /// </summary>
        /// <param name="filePath">The path of the file to open</param>
        /// <returns>The number of game entries found</returns>
        public int ImportSteamFile( string filePath, SortedSet<int> ignore, bool ignoreDlc ) {
            Program.Logger.Write(LoggerLevel.Info, GlobalStrings.GameData_OpeningSteamConfigFile, filePath);
            TextVdfFileNode dataRoot;

            try {
                using( StreamReader reader = new StreamReader( filePath, false ) ) {
                    dataRoot = TextVdfFileNode.Load( reader, true );
                }
            } catch( ParseException e ) {
                Program.Logger.Write(LoggerLevel.Error, GlobalStrings.GameData_ErrorParsingConfigFileParam, e.Message);
                throw new ApplicationException(GlobalStrings.GameData_ErrorParsingSteamConfigFile + e.Message, e);
            } catch( IOException e ) {
                Program.Logger.Write(LoggerLevel.Error, GlobalStrings.GameData_ErrorOpeningConfigFileParam, e.Message);
                throw new ApplicationException(GlobalStrings.GameData_ErrorOpeningSteamConfigFile + e.Message, e);
            }

            TextVdfFileNode appsNode = dataRoot.GetNodeAt( new string[] { "Software", "Valve", "Steam", "apps" }, true );
            int count = GetDataFromVdf( appsNode, ignore, ignoreDlc );
            Program.Logger.Write(LoggerLevel.Info, GlobalStrings.GameData_SteamConfigFileLoaded, count);
            return count;
        }

        /// <summary>
        /// Loads in games from an node containing a list of games.
        /// </summary>
        /// <param name="appsNode">Node containing the game nodes</param>
        /// <param name="ignore">Set of games to ignore</param>
        /// <returns>Number of games loaded</returns>
        private int GetDataFromVdf( TextVdfFileNode appsNode, SortedSet<int> ignore, bool ignoreDlc ) {
            int loadedGames = 0;

            Dictionary<string, TextVdfFileNode> gameNodeArray = appsNode.NodeArray;
            if( gameNodeArray != null ) {
                foreach( KeyValuePair<string, TextVdfFileNode> gameNodePair in gameNodeArray ) {
                    int gameId;
                    if( int.TryParse( gameNodePair.Key, out gameId ) ) {
                        if( ( ignore != null && ignore.Contains( gameId ) ) || ( ignoreDlc && Program.GameDB.IsDlc( gameId ) ) ) {
                            Program.Logger.Write(LoggerLevel.Verbose, GlobalStrings.GameData_SkippedProcessingGame, gameId);
                            continue;
                        }
                        if( gameNodePair.Value != null && gameNodePair.Value.ContainsKey( "tags" ) ) {
                            Category cat = null;
                            bool fav = false;
                            loadedGames++;
                            TextVdfFileNode tagsNode = gameNodePair.Value["tags"];
                            Dictionary<string, TextVdfFileNode> tagArray = tagsNode.NodeArray;
                            if( tagArray != null ) {
                                foreach( TextVdfFileNode tag in tagArray.Values ) {
                                    string tagName = tag.NodeString;
                                    if( tagName != null ) {
                                        if( tagName == "favorite" ) {
                                            fav = true;
                                        } else {
                                            cat = GetCategory( tagName );
                                        }
                                    }
                                }
                            }

                            if( !Games.ContainsKey( gameId ) ) {
                                Game newGame = new Game( gameId, string.Empty );
                                Games.Add( gameId, newGame );
                                newGame.Name = Program.GameDB.GetName( gameId );
                                Program.Logger.Write(LoggerLevel.Verbose, GlobalStrings.GameData_AddedNewGame, gameId, newGame.Name);
                            }
                            Games[gameId].Category = cat;
                            Games[gameId].Favorite = fav;
                            Program.Logger.Write(LoggerLevel.Verbose, GlobalStrings.GameData_ProcessedGame, gameId, (cat == null) ? "~none~" : cat.ToString(), fav);
                        }
                    }
                }
            }

            return loadedGames;
        }

        /// <summary>
        /// Writes category information out to a steam config file. Also saves any other settings that had been loaded, to avoid setting loss.
        /// </summary>
        /// <param name="path">Full path of the steam config file to save</param>
        public void SaveSteamFile( string filePath, bool discardMissing ) {
            Program.Logger.Write(LoggerLevel.Info, GlobalStrings.GameData_SavingSteamConfigFile, filePath);

            TextVdfFileNode fileData = new TextVdfFileNode();
            try {
                using( StreamReader reader = new StreamReader( filePath, false ) ) {
                    fileData = TextVdfFileNode.Load( reader, true );
                }
            } catch( Exception e ) {
                Program.Logger.Write(LoggerLevel.Warning, GlobalStrings.GameData_LoadingErrorSteamConfig, e.Message);
            }

            TextVdfFileNode appListNode = fileData.GetNodeAt( new string[] { "Software", "Valve", "Steam", "apps" }, true );

            if( discardMissing ) {
                Dictionary<string, TextVdfFileNode> gameNodeArray = appListNode.NodeArray;
                if( gameNodeArray != null ) {
                    foreach( KeyValuePair<string, TextVdfFileNode> pair in gameNodeArray ) {
                        int gameId;
                        if( !( int.TryParse( pair.Key, out gameId ) && Games.ContainsKey( gameId ) ) ) {
                            Program.Logger.Write(LoggerLevel.Verbose, GlobalStrings.GameData_RemovingGameCategoryFromSteamConfig, gameId);
                            pair.Value.RemoveSubnode( "tags" );
                        }
                    }
                }
            }

            foreach( Game game in Games.Values ) {
                Program.Logger.Write(LoggerLevel.Verbose, GlobalStrings.GameData_AddingGameToConfigFile, game.Id);
                TextVdfFileNode gameNode = appListNode[game.Id.ToString()];
                gameNode.RemoveSubnode( "tags" );
                if( game.Category != null || game.Favorite ) {
                    TextVdfFileNode tagsNode = gameNode["tags"];
                    int key = 0;
                    if( game.Category != null ) {
                        tagsNode[key.ToString()] = new TextVdfFileNode( game.Category.Name );
                        key++;
                    }
                    if( game.Favorite ) {
                        tagsNode[key.ToString()] = new TextVdfFileNode( "favorite" );
                    }
                }
            }

            Program.Logger.Write(LoggerLevel.Verbose, GlobalStrings.GameData_CleaningUpSteamConfigTree);
            appListNode.CleanTree();

            Program.Logger.Write(LoggerLevel.Info, GlobalStrings.GameData_WritingToDisk);
            TextVdfFileNode fullFile = new TextVdfFileNode();
            fullFile["UserLocalConfigStore"] = fileData;
            try {
                FileInfo f = new FileInfo( filePath );
                f.Directory.Create();
                FileStream fStream = f.Open( FileMode.Create, FileAccess.Write, FileShare.None );
                using( StreamWriter writer = new StreamWriter( fStream ) ) {
                    fullFile.Save( writer );
                }
                fStream.Close();
            } catch( ArgumentException e ) {
                Program.Logger.Write(LoggerLevel.Error, GlobalStrings.GameData_ErrorSavingSteamConfigFile, e.ToString());
                throw new ApplicationException(GlobalStrings.GameData_FailedToSaveSteamConfigBadPath, e);
            } catch( IOException e ) {
                Program.Logger.Write(LoggerLevel.Error, GlobalStrings.GameData_ErrorSavingSteamConfigFile, e.ToString());
                throw new ApplicationException(GlobalStrings.GameData_FailedToSaveSteamConfigFile + e.Message, e);
            } catch( UnauthorizedAccessException e ) {
                Program.Logger.Write(LoggerLevel.Error, GlobalStrings.GameData_ErrorSavingSteamConfigFile, e.ToString());
                throw new ApplicationException(GlobalStrings.GameData_AccessDeniedSteamConfigFile + e.Message, e);
            }
        }
    }

    class ProfileAccessException : ApplicationException {
        public ProfileAccessException( string m ) : base( m ) { }
    }
}