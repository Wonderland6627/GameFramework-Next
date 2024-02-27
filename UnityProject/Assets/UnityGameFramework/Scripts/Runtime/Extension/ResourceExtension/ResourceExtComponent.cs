﻿using System.Collections;
using System.Collections.Generic;
using GameFramework;
using GameFramework.ObjectPool;
using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace UnityGameFramework.Runtime
{
    /// <summary>
    /// 资源组件拓展。
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Game Framework/ResourceExt")]
    public partial class ResourceExtComponent : GameFrameworkComponent
    {
        /// <summary>
        /// 检查是否可以释放间隔
        /// </summary>
        [SerializeField] private float m_CheckCanReleaseInterval = 30f;

        private float m_CheckCanReleaseTime = 0.0f;

        /// <summary>
        /// 对象池自动释放时间间隔
        /// </summary>
        [SerializeField] private float m_AutoReleaseInterval = 60f;

        /// <summary>
        /// 保存加载的图片对象
        /// </summary>
#if ODIN_INSPECTOR
        [ShowInInspector]
#endif
        private LinkedList<LoadAssetObject> m_LoadAssetObjectsLinkedList;

        /// <summary>
        /// 散图集合对象池
        /// </summary>
        private IObjectPool<AssetItemObject> m_AssetItemPool;


#if UNITY_EDITOR
        public LinkedList<LoadAssetObject> LoadAssetObjectsLinkedList
        {
            get => m_LoadAssetObjectsLinkedList;
            set => m_LoadAssetObjectsLinkedList = value;
        }
#endif
        private IEnumerator Start()
        {
            yield return new WaitForEndOfFrame();
            ObjectPoolComponent objectPoolComponent = GameSystem.GetComponent<ObjectPoolComponent>();
            m_AssetItemPool = objectPoolComponent.CreateMultiSpawnObjectPool<AssetItemObject>(
                "SetAssetPool",
                m_AutoReleaseInterval, 16, 60, 0);
            m_LoadAssetObjectsLinkedList = new LinkedList<LoadAssetObject>();
            
            InitializedResources();
        }

        private void Update()
        {
            m_CheckCanReleaseTime += Time.unscaledDeltaTime;
            if (m_CheckCanReleaseTime < (double)m_CheckCanReleaseInterval)
            {
                return;
            }

            ReleaseUnused();
        }

        /// <summary>
        /// 回收无引用的缓存资产。
        /// </summary>
#if ODIN_INSPECTOR
        [Button("Release Unused")]
#endif
        public void ReleaseUnused()
        {
            if (m_LoadAssetObjectsLinkedList == null)
            {
                return;
            }

            LinkedListNode<LoadAssetObject> current = m_LoadAssetObjectsLinkedList.First;
            while (current != null)
            {
                var next = current.Next;
                if (current.Value.AssetObject.IsCanRelease())
                {
                    m_AssetItemPool.Unspawn(current.Value.AssetTarget);
                    ReferencePool.Release(current.Value.AssetObject);
                    m_LoadAssetObjectsLinkedList.Remove(current);
                }

                current = next;
            }

            m_CheckCanReleaseTime = 0f;
        }

        private void SetAsset(ISetAssetObject setAssetObject, Object assetObject)
        {
            m_LoadAssetObjectsLinkedList.AddLast(new LoadAssetObject(setAssetObject, assetObject));
            setAssetObject.SetAsset(assetObject);
        }
    }
}