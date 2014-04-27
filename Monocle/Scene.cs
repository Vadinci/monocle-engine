﻿using Microsoft.Xna.Framework;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Monocle
{
    public class Scene : IEnumerable<Entity>, IEnumerable
    {
        public float TimeActive { get; private set; }
        public bool Focused { get; private set; }
        public EntityList Entities { get; private set; }
        public EntityList[] TagLists { get; private set; }
        public List<Renderer> Renderers { get; private set; }

        private Dictionary<int, float> actualDepthLookup;

        public Scene()
        {
            Entities = new EntityList(this);
            TagLists = new EntityList[Engine.MAX_TAG];
            Renderers = new List<Renderer>();

            actualDepthLookup = new Dictionary<int, float>();
        }

        public virtual void Begin()
        {
            Focused = true;
            foreach (var entity in Entities)
                entity.SceneBegin();
        }

        public virtual void End()
        {
            Focused = false;
            foreach (var entity in Entities)
                entity.SceneEnd();
        }

        public virtual void Update()
        {
            TimeActive += Engine.DeltaTime;

            SetEntityListLockMode(EntityList.LockModes.Locked);
            foreach (var entity in Entities)
                if (entity.Active)
                    entity.Update();
            SetEntityListLockMode(EntityList.LockModes.Open);
        }

        public virtual void BeforeRender()
        {
            SetEntityListLockMode(EntityList.LockModes.Error);

            foreach (var renderer in Renderers)
            {
                Draw.Renderer = renderer;
                renderer.BeforeRender(this);
            }
        }

        public virtual void Render()
        {
            foreach (var renderer in Renderers)
            {
                Draw.Renderer = renderer;
                renderer.Render(this);
            }
        }

        public virtual void AfterRender()
        {
            foreach (var renderer in Renderers)
            {
                Draw.Renderer = renderer;
                renderer.AfterRender(this);
            }

            Draw.Renderer = null;
            SetEntityListLockMode(EntityList.LockModes.Open);
        }

        public virtual void HandleGraphicsReset()
        {
            foreach (var entity in Entities)
                entity.HandleGraphicsReset();
        }

        public void RenderAllEntities()
        {
            foreach (var entity in Entities)
                if (entity.Visible)
                    entity.Render();
        }

        public void RenderTaggedEntities(int tag)
        {
            foreach (var entity in TagLists[tag])
                if (entity.Visible)
                    entity.Render();
        }

        /// <summary>
        /// Returns whether the Scene timer has passed the given time interval since the last frame. Ex: given 2.0f, this will return true once every 2 seconds
        /// </summary>
        /// <param name="interval">The time interval to check for</param>
        /// <returns></returns>
        public bool OnInterval(float interval)
        {
            return (int)((TimeActive - Engine.DeltaTime) / interval) < (int)(TimeActive / interval);
        }

        #region Collisions

        public bool CollideCheck(Vector2 point, int tag)
        {
            foreach (var e in TagLists[(int)tag])
                if (e.Collidable && e.CollideCheck(point))
                    return true;
            return false;
        }

        public bool CollideCheck(Vector2 from, Vector2 to, int tag)
        {
            foreach (var e in TagLists[(int)tag])
                if (e.Collidable && e.CollideLine(from, to))
                    return true;
            return false;
        }

        public bool CollideCheck(Rectangle rect, int tag)
        {
            foreach (var e in TagLists[(int)tag])
                if (e.Collidable && e.CollideCheck(rect))
                    return true;
            return false;
        }

        public Entity CollideFirst(Vector2 point, int tag)
        {
            foreach (var e in TagLists[(int)tag])
                if (e.Collidable && e.CollideCheck(point))
                    return e;
            return null;
        }

        public Entity CollideFirst(Vector2 from, Vector2 to, int tag)
        {
            foreach (var e in TagLists[(int)tag])
                if (e.Collidable && e.CollideLine(from, to))
                    return e;
            return null;
        }

        public Entity CollideFirst(Rectangle rect, int tag)
        {
            foreach (var e in TagLists[(int)tag])
                if (e.Collidable && e.CollideCheck(rect))
                    return e;
            return null;
        }

        public void CollideInto(Vector2 point, int tag, List<Entity> list)
        {
            foreach (var e in TagLists[(int)tag])
                if (e.Collidable && e.CollideCheck(point))
                    list.Add(e);
        }

        public void CollideInto(Vector2 from, Vector2 to, int tag, List<Entity> list)
        {
            foreach (var e in TagLists[(int)tag])
                if (e.Collidable && e.CollideLine(from, to))
                    list.Add(e);
        }

        public void CollideInto(Rectangle rect, int tag, List<Entity> list)
        {
            foreach (var e in TagLists[(int)tag])
                if (e.Collidable && e.CollideCheck(rect))
                    list.Add(e);
        }

        public List<Entity> CollideAll(Vector2 point, int tag)
        {
            List<Entity> list = new List<Entity>();
            CollideInto(point, tag, list);
            return list;
        }

        public List<Entity> CollideAll(Vector2 from, Vector2 to, int tag)
        {
            List<Entity> list = new List<Entity>();
            CollideInto(from, to, tag, list);
            return list;
        }

        public List<Entity> CollideAll(Rectangle rect, int tag)
        {
            List<Entity> list = new List<Entity>();
            CollideInto(rect, tag, list);
            return list;
        }

        public void CollideDo(Vector2 point, int tag, Action<Entity> action)
        {
            foreach (var e in TagLists[(int)tag])
                if (e.Collidable && e.CollideCheck(point))
                    action(e);
        }

        public void CollideDo(Vector2 from, Vector2 to, int tag, Action<Entity> action)
        {
            foreach (var e in TagLists[(int)tag])
                if (e.Collidable && e.CollideLine(from, to))
                    action(e);
        }

        public void CollideDo(Rectangle rect, int tag, Action<Entity> action)
        {
            foreach (var e in TagLists[(int)tag])
                if (e.Collidable && e.CollideCheck(rect))
                    action(e);
        }

        public Vector2 LineCheck(Vector2 from, Vector2 to, int tag, float precision)
        {
            Vector2 add = to - from;
            add.Normalize();
            add *= precision;

            int amount = (int)Math.Floor((from - to).Length() / precision);
            Vector2 prev = from;
            Vector2 at = from + add;

            for (int i = 0; i <= amount; i++)
            {
                if (CollideCheck(at, tag))
                    return prev;
                prev = at;
                at += add;
            }

            return to;
        }

        #endregion

        #region Utils

        private void SetEntityListLockMode(EntityList.LockModes lockMode)
        {
            Entities.LockMode = lockMode;
            foreach (var list in TagLists)
                if (list != null)
                    list.LockMode = lockMode;
        }

        internal void SetActualDepth(Entity entity)
        {
            const float theta = .000001f;

            float add = 0;
            if (actualDepthLookup.TryGetValue(entity.depth, out add))
                actualDepthLookup[entity.depth] += theta;
            else
                actualDepthLookup.Add(entity.depth, theta);
            entity.actualDepth = entity.depth - add;

            //Mark lists unsorted
            Entities.MarkUnsorted();
            foreach (var tag in entity.Tags)
                TagLists[tag].MarkUnsorted();
        }

        internal void TagEntity(int tag, Entity entity)
        {
            if (TagLists[tag] == null)
                TagLists[tag] = new EntityList();
            TagLists[tag].Add(entity);
        }

        #endregion

        #region Entity Shortcuts

        /// <summary>
        /// Quick access to entire tag lists of Entities. Result will never be null
        /// </summary>
        /// <param name="tag">The tag list to fetch</param>
        /// <returns></returns>
        public EntityList this[int tag]
        {
            get
            {
                if (TagLists[(int)tag] == null)
                    TagLists[(int)tag] = new EntityList();
                return TagLists[(int)tag];
            }
        }

        /// <summary>
        /// Shortcut function for adding an Entity to the Scene's Entities list
        /// </summary>
        /// <param name="entity">The Entity to add</param>
        public void Add(Entity entity)
        {
            Entities.Add(entity);
        }

        /// <summary>
        /// Shortcut function for removing an Entity from the Scene's Entities list
        /// </summary>
        /// <param name="entity">The Entity to remove</param>
        public void Remove(Entity entity)
        {
            Entities.Remove(entity);
        }

        /// <summary>
        /// Shortcut function for adding a set of Entities from the Scene's Entities list
        /// </summary>
        /// <param name="entities">The Entities to add</param>
        public void Add(IEnumerable<Entity> entities)
        {
            Entities.Add(entities);
        }

        /// <summary>
        /// Shortcut function for removing a set of Entities from the Scene's Entities list
        /// </summary>
        /// <param name="entities">The Entities to remove</param>
        public void Remove(IEnumerable<Entity> entities)
        {
            Entities.Remove(entities);
        }

        /// <summary>
        /// Shortcut function for adding a set of Entities from the Scene's Entities list
        /// </summary>
        /// <param name="entities">The Entities to add</param>
        public void Add(params Entity[] entities)
        {
            Entities.Add(entities);
        }

        /// <summary>
        /// Shortcut function for removing a set of Entities from the Scene's Entities list
        /// </summary>
        /// <param name="entities">The Entities to remove</param>
        public void Remove(params Entity[] entities)
        {
            Entities.Remove(entities);
        }

        /// <summary>
        /// Allows you to iterate through all Entities in the Scene
        /// </summary>
        /// <returns></returns>
        public IEnumerator<Entity> GetEnumerator()
        {
            return Entities.GetEnumerator();
        }

        /// <summary>
        /// Allows you to iterate through all Entities in the Scene
        /// </summary>
        /// <returns></returns>
        IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        #region Renderer Shortcuts

        /// <summary>
        /// Shortcut function to add a Renderer to the Renderer list
        /// </summary>
        /// <param name="renderer">The Renderer to add</param>
        public void Add(Renderer renderer)
        {
            Renderers.Add(renderer);
        }

        /// <summary>
        /// Shortcut function to remove a Renderer from the Renderer list
        /// </summary>
        /// <param name="renderer">The Renderer to remove</param>
        public void Remove(Renderer renderer)
        {
            Renderers.Add(renderer);
        }

        #endregion
    }
}
