using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace WordGame
{
    [System.Serializable]
    public struct WordData
    {
        public Vector3Int cellPos;
        public string word;
        public WordCreationDirectionE dir;
    }
    
    public class LevelGenerator : MonoBehaviour
    {
        [SerializeField] TextAsset levelText;
        [SerializeField] Grid grid;
        [SerializeField] Tile tilePrefab; 
        [SerializeField] Vector2 buffer;
        readonly List<Tile> tiles= new List<Tile>();
        public List<WordData> wordDataList = new List<WordData>();
        [SerializeField] InputHandler inputHandler;

        
        
        void Start()
        {
            int columns = 0;
            int rows = 0;
            
            string fileContent = levelText.text;
            

            // Convert the TextAsset content to a StringReader
            using (StringReader reader = new StringReader(fileContent))
            {
                string line;

                // Read each line and print it
                while ((line = reader.ReadLine()) != null)
                {
                    var parts = line.Split(" ");
                    if (parts[0] == LevelDataSaver.dimensionIndicator)
                    {
                        var lineSegments = line.Split(" ");
                        columns = int.Parse(lineSegments[1]);
                        rows = int.Parse(lineSegments[2]);
                    }
                    else if (parts[0] == LevelDataSaver.emptyTileIndicator)
                    {
                        var lineSegments = line.Split(" ");
                        var cellPos = new Vector3Int(int.Parse(lineSegments[1]), int.Parse(lineSegments[2]), 0);
                        var tileWorldPos = grid.GetCellCenterWorld(cellPos);
                        var tile = Instantiate(tilePrefab, tileWorldPos, Quaternion.identity, transform);
                        tile.cellPos = cellPos;
                        // tile.SetLetter(lineSegments[3].ToCharArray()[0]);
                        tiles.Add(tile);
                        tile.gameObject.SetActive(false);
                    }
                    else if (parts[0] == LevelDataSaver.fullTileIndicator)
                    {
                        var lineSegments = line.Split(" ");
                        var cellPos = new Vector3Int(int.Parse(lineSegments[1]), int.Parse(lineSegments[2]), 0);
                        var tileWorldPos = grid.GetCellCenterWorld(cellPos);
                        var tile = Instantiate(tilePrefab, tileWorldPos, Quaternion.identity, transform);
                        tile.cellPos = cellPos;
                        tile.SetLetter(lineSegments[3].ToCharArray()[0]);
                        tile.HideLetter();
                        tiles.Add(tile);
                    }
                    else if (parts[0] == LevelDataSaver.wordIndicator)
                    {
                        var lineSegments = line.Split(" ");
                        var wordData = new WordData();
                        wordData.word = lineSegments[1];
                        var numbers = lineSegments[2].Split(",");
                        wordData.cellPos = new Vector3Int(int.Parse(numbers[0]), int.Parse(numbers[1]));
                        if (lineSegments[3] == "down")
                        {
                            wordData.dir = WordCreationDirectionE.down;
                        }
                        else if (lineSegments[3] == "right")
                        {
                            wordData.dir = WordCreationDirectionE.right;
                        }
                        wordDataList.Add(wordData);
                    }
                    // Debug.Log("Line: " + line.Trim()); // Trim removes leading and trailing whitespaces
                }
            }
            
            SetCam(rows, columns);
            
            Game.onLevelGenerated.Invoke(wordDataList);
        }

        public bool TryMatchWord(string word)
        {
            var x = wordDataList.FirstOrDefault(w =>  w.word == word);
            if (x.word == default)
            {
                return false;
            }

            StartCoroutine(RevealWord(x));
            
            return true;
        }

        IEnumerator RevealWord(WordData data)
        {
            Vector3Int dir = Vector3Int.zero;

            switch (data.dir)
            {
                case WordCreationDirectionE.none:
                    break;
                case WordCreationDirectionE.right:
                    dir += Vector3Int.right;
                    break;
                case WordCreationDirectionE.down:
                    dir += Vector3Int.down;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            var pos = data.cellPos;
                
            for (int i = 0; i < data.word.Length; i++)
            {
                if (TryGetTileInTheCell(out Tile tile, pos))
                {
                    tile.RevealLetter();
                    yield return null;
                }

                pos += dir;
            }
        }
        
        bool TryGetTileInTheCell(out Tile tile, Vector3Int cell)
        {
            foreach (Tile t in tiles)
            {
                if (t.cellPos.x == cell.x && t.cellPos.y == cell.y)
                {
                    tile = t;
                    return true;
                }
            }

            tile = null;
            return false;
        }
        
        void SetCam(int rows, int columns)
        {
            Bounds bounds = new Bounds();
            foreach (Tile tile in tiles)
            {
                bounds.Encapsulate(tile.spriteRenderer.bounds);
            }

            Bounds boundsBeforeBuffer = bounds;
            bounds.Expand(new Vector3(buffer.x, buffer.y, 0));
            
            var gridTopRightPos = grid.CellToWorld(new Vector3Int(columns, rows));
            var camPos = gridTopRightPos / 2;
            
            // onGridCreated.Invoke(new GridCreatedEventData());
            Game.onGridCreated.Invoke(new OnGridCreated.Data(camPos, bounds, boundsBeforeBuffer));
        }
    }
}
