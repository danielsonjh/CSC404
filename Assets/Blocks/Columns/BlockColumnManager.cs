﻿using System;
using System.Collections;
using UnityEngine;

[ExecuteInEditMode]
public class BlockColumnManager : MonoBehaviour
{
    public const int Depth = 3;
    public const int Width = 10;
    public const int WallZIndex = 1;
    public const int BlueTeamZIndex = 0;
    public const int PurpleTeamZIndex = 2;
    public const float SlideBlockDuration = 0.25f;

    public static BlockColumnManager Instance;

    public readonly BlockColumn[,] BlockColumns = new BlockColumn[Width, Depth];
    
    public float SupportBlockHeight { get { return transform.position.y + _supportBoxCollider.center.y; } }

    public GameObject BlockColumnPrefab;
    public GameObject BlockPrefab;
    public GameObject ImmovableBlockPrefab;
    public GameObject BombBlockPrefab;

    private BoxCollider _supportBoxCollider;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        _supportBoxCollider = GetComponent<BoxCollider>();
        _supportBoxCollider.center = new Vector3((Width - 1) / 2f, -1, (Depth - 1) / 2f);

        while (transform.childCount > 0)
        {
            var child = transform.GetChild(0);
            DestroyImmediate(child.gameObject);
        }

        transform.position = new Vector3(-Width / 2f, 0, -(Depth - 1) / 2f);
        for (var x = 0; x < Width; x++)
        {
            for (var z = 0; z < Depth; z++)
            {
                var blockColumn = Instantiate(BlockColumnPrefab);
                blockColumn.transform.SetParent(transform);
                blockColumn.transform.localPosition = new Vector3(x, 0, z);
                var blockColumnComponent = blockColumn.GetComponent<BlockColumn>();
                blockColumnComponent.Initialize();
                BlockColumns[x, z] = blockColumnComponent;
                    
                if (z == WallZIndex)
                {
                    blockColumnComponent.gameObject.AddComponent(typeof(BlockWallGenerator));
                    blockColumnComponent.BaseColor = Block.NeutralColor;
                }
                else
                {
                    blockColumnComponent.BaseColor = z == BlueTeamZIndex ? Block.BlueColor : Block.PurpleColor;
                }

                var block = Instantiate(BlockPrefab);
                block.transform.position = transform.position + new Vector3(x, 0, z);
                blockColumnComponent.Add(block);
                var blockComponent = block.GetComponent<Block>();
                blockComponent.Initialize();
                blockComponent.MakeFallImmediately();
            }
        }
    }

    public Vector3 GetRespawnPoint(Team team)
    {
        var z = team == Team.Blue ? BlueTeamZIndex : PurpleTeamZIndex;
        var highestColumn = BlockColumns[0, z];
        for (var x = 1; x < Width; x++)
        {
            if (BlockColumns[x, z].Blocks.Count > highestColumn.Blocks.Count)
            {
                highestColumn = BlockColumns[x, z];
            }
        }

        var highestBlock = highestColumn.Blocks[highestColumn.Blocks.Count - 1];
        return highestBlock.transform.position + Vector3.up;
    }

    public void SlideBlock(GameObject block, Vector3 direction)
    {
        StartCoroutine(SlideBlockCoroutine(block, direction));
    }

    public void MoveSupportUp()
    {
        _supportBoxCollider.center += Vector3.up;
    }

    private IEnumerator SlideBlockCoroutine(GameObject block, Vector3 direction)
    {
        
        var oldBlockColumn = GetBlockColumnAtLocalPosition(block.transform.parent.localPosition);
        var newBlockColumn = GetBlockColumnAtLocalPosition(block.transform.parent.localPosition + direction);
        var removedBlock = oldBlockColumn.Remove(block.transform.position);

        var t = 0f;
        var oldPosition = removedBlock.transform.position;

        while (t <= SlideBlockDuration)
        {
            removedBlock.transform.position = Vector3.Lerp(oldPosition, oldPosition + direction, t / SlideBlockDuration);
            yield return new WaitForEndOfFrame();
            t += Time.deltaTime;
        }

        removedBlock.transform.position = oldPosition + direction;
        removedBlock.transform.position = removedBlock.transform.position.RoundToInt();

        if (newBlockColumn != null)
        {
            newBlockColumn.Add(removedBlock);
        }

        //if bomb block, make active, start exploding coroutine

        if (removedBlock.tag == "BombBlock")
        {
            Debug.Log("FIRE IN THE HOLE");
            StartCoroutine(BombExplodeCoroutine(newBlockColumn, removedBlock));

        }

    }

    private IEnumerator BombExplodeCoroutine(BlockColumn blockColumn, GameObject block)
    {
        // Probably won't need this Coroutine anymore
        // Just replace with the following:
        block.GetComponent<BombBlock>().SetBombActive();

         
        yield return new WaitForSeconds(1.0f);

        /*
        yield return new WaitForSeconds(1.0f);
        destroyBlock(blockColumn, block.transform.position);
        destroyBlock(blockColumn, block.transform.position + transform.up);
        destroyBlock(blockColumn, block.transform.position - transform.up);
        //Debug.Log(blockColumn.transform.localPosition);
        
        try
        {
            var columnLeft = GetBlockColumnAtLocalPosition(blockColumn.transform.localPosition - transform.right);
            destroyBlock(columnLeft, block.transform.position - transform.right);
            destroyBlock(columnLeft, block.transform.position + transform.up - transform.right);
            destroyBlock(columnLeft, block.transform.position - transform.up - transform.right);
        }
        catch (Exception)
        {
            Debug.Log(String.Format("no column at {0}", blockColumn.transform.localPosition - transform.right));
        }

        try
        {
            var columnRight = GetBlockColumnAtLocalPosition(blockColumn.transform.localPosition + transform.right);
            destroyBlock(columnRight, block.transform.position + transform.right);
            destroyBlock(columnRight, block.transform.position + transform.up + transform.right);
            destroyBlock(columnRight, block.transform.position - transform.up + transform.right);
        }
        catch (Exception)
        {
            Debug.Log(String.Format("no column at {0}", blockColumn.transform.localPosition + transform.right));
        }
        */
    }

    private bool destroyBlock(BlockColumn blockColumn, Vector3 blockPosition)
    {
        try
        {
            var destroyedBlock = blockColumn.Remove(blockPosition);
            Destroy(destroyedBlock);
            Debug.Log(String.Format("removed block at {0}", destroyedBlock.transform.position));
        }
        catch (ArgumentException)
        {
            Debug.Log(String.Format("no block to remove at {0}", blockPosition));
            return false;
        }

        return true;
    }

    private BlockColumn GetBlockColumnAtLocalPosition(Vector3 localPosition)
    {
        try
        {
            return BlockColumns[Mathf.RoundToInt(localPosition.x), Mathf.RoundToInt(localPosition.z)];
        }
        catch (IndexOutOfRangeException)
        {
            return null;
        }
    }
}