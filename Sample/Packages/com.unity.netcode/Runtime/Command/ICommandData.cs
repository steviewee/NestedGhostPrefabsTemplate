using System;
using System.Runtime.CompilerServices;
using System.Text;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.NetCode.LowLevel.Unsafe;
using UnityEngine;

namespace Unity.NetCode
{
    /// <summary>
    /// When using MaxPredictionStepBatchSize the client will batch
    /// prediction steps, but it will normally break the batches when input changes.
    /// If this attribute is placed on an input in the ICommandData / IInputComponentData
    /// component changes to the field marked with this attribute will not break batches.
    /// This can be used for example to make sure mouse look input changes can be batched
    /// while starting to move cannot be batched.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field|AttributeTargets.Property)]
    public class BatchPredictAttribute : Attribute
    {}

    /// <summary>
    /// <para>Commands (usually inputs) that must be sent from client to server to control an entity (or any other thing)
    /// should implement the ICommandData interface.</para>
    ///
    /// <para>Prefer using the ICommandData over Rpc if you need to send a constant
    /// stream of data from client to server, as it's optimized for this use-case.</para>
    ///
    /// <para>Prefer to keep this type as small as possible, as it scales exponentially with player count and tickrate.</para>
    ///
    /// <para>ICommandData, being a subclass of <see cref="IBufferElementData"/>, can also be serialized from the server to the clients.
    /// It also natively supports the presence of the <see cref="GhostComponentAttribute"/> and <see cref="GhostFieldAttribute"/> attributes.
    /// As such, the same rule for buffers apply: if the command buffer must be serialized, then all fields must be annotated
    /// with a <see cref="GhostFieldAttribute"/>. Failure to do so will generate code-generation errors.</para>
    ///
    /// <para>However, differently from a normal GhostComponent, ICommandData buffers are not replicated from the server to all clients by default.
    /// Instead, in the absence of a GhostComponentAttribute governing the serialization behavior, the following set of default rules are used:</para>
    ///
    /// <para>- <see cref="GhostComponentAttribute.PrefabType"/> is set to <see cref="GhostPrefabType.All"/>. The buffer is present on all the
    /// ghost variant.</para>
    /// <para>- <see cref="GhostComponentAttribute.SendTypeOptimization"/> is set to <see cref="GhostSendType.OnlyPredictedClients"/>. Only predicted ghost
    /// can receive the buffer and interpolated variant will have the component stripped or disabled.</para>
    /// <para>- <see cref="GhostComponentAttribute.OwnerSendType"/> is set to <see cref="SendToOwnerType.SendToNonOwner"/>. If the ghost
    /// has an owner, is sent only to the clients who don't own the ghost.</para>
    ///
    /// <para>Is generally not recommended to send back to the ghost owner its own commands. For that reason, setting the
    /// <see cref="SendToOwnerType.SendToOwner"/> flag will be reported as a error and ignored.
    /// Also, because they way ICommandData works, some care must be used when setting the <see cref="GhostComponentAttribute.PrefabType"/>
    /// property:</para>
    ///
    /// <para>- Server: While possible, does not make much sense. A warning will be reported.</para>
    /// <para>- Clients: The ICommandData buffer is stripped from the server ghost. A warning will be reported.</para>
    /// <para>- InterpolatedClient: ICommandData buffers are stripped from the server and predicted ghost. A warning will be reported.</para>
    /// <para>- Predicted: ICommandData buffers are stripped from the server and predicted ghost. A warning will be reported.</para>
    /// <para>- <b>AllPredicted: Interpolated ghost will not have the command buffer.</b></para>
    /// <para>- <b>All: All ghost will have the command buffer.</b></para>
    /// </summary>
    public interface ICommandData : IBufferElementData
    {
        /// <summary>
        /// The tick the command should be executed. It is mandatory to set the tick before adding the command to the
        /// buffer using <see cref="CommandDataUtility.AddCommandData{T}"/>.
        /// </summary>
        [DontSerializeForCommand]
        NetworkTick Tick { get; set; }

        /// <summary>
        /// Implement this to get Burst-compatible input struct packet dump logging.
        /// Recommended format: $"field1:{field1}, field2:{field2}";
        /// </summary>
        /// <remarks>This function must be burst compatible too, otherwise you'll get burst compiler errors.</remarks>
        /// <returns>Field values of your input struct.</returns>
        public FixedString512Bytes ToFixedString() => "?ICD?";
    }

    /// <summary>
    /// Interface that must be implemented to serialize/deserialize <see cref="ICommandData"/>.
    /// Usually commands serialization / deserialization is automatically generated, unless a
    /// <see cref="NetCodeDisableCommandCodeGenAttribute"/> is added to the command struct to opt-in for manual serializaton.
    /// If you enable manual serializaton, you must create a public struct that implement the ICommandDataSerializer for your type, as
    /// well as the necessary send and received systems in order to have your RPC sent and received.
    /// </summary>
    /// <typeparam name="T">Your data type.</typeparam>
    public interface ICommandDataSerializer<T> where T: unmanaged, ICommandData
    {
        /// <summary>
        /// Serialize the command to the data stream.
        /// </summary>
        /// <param name="writer">An instance of a <see cref="DataStreamWriter"/></param>
        /// <param name="state">An instance of <see cref="RpcSerializerState"/> used to carry some additional data and accessor
        /// for serializing the command field type. In particular, used to serialize entity</param>
        /// <param name="data">Command</param>
        void Serialize(ref DataStreamWriter writer, in RpcSerializerState state, in T data);
        /// <summary>
        /// Deserialize a single command from the data stream.
        /// </summary>
        /// <param name="reader">An instance of a <see cref="DataStreamWriter"/></param>
        /// <param name="state">An instance of <see cref="RpcSerializerState"/> used to carry some additional data and accessor
        /// for serializing the command field type. In particular, used to serialize entity</param>
        /// <param name="data">Command</param>
        void Deserialize(ref DataStreamReader reader, in RpcDeserializerState state, ref T data);

        /// <summary>
        /// Serialize the command to the data stream using delta compression.
        /// </summary>
        /// <param name="writer">An instance of a <see cref="DataStreamWriter"/></param>
        /// <param name="state">An instance of <see cref="RpcSerializerState"/> used to carry some additional data and accessor
        /// for serializing the command field type. In particular, used to serialize entity</param>
        /// <param name="data">Command</param>
        /// <param name="baseline">Baseline command</param>
        /// <param name="compressionModel">Delta compression model</param>
        void Serialize(ref DataStreamWriter writer, in RpcSerializerState state, in T data, in T baseline, StreamCompressionModel compressionModel);

        /// <summary>
        /// Deserialize a single command from the data stream using delta compression
        /// </summary>
        /// <param name="reader">An instance of a <see cref="DataStreamWriter"/></param>
        /// <param name="state">An instance of <see cref="RpcSerializerState"/> used to carry some additional data and accessor
        /// for serializing the command field type. In particular, used to serialize entity</param>
        /// <param name="data">Command</param>
        /// <param name="baseline">Baseline command</param>
        /// <param name="compressionModel">Delta compression model</param>
        void Deserialize(ref DataStreamReader reader, in RpcDeserializerState state, ref T data, in T baseline, StreamCompressionModel compressionModel);

        /// <summary>
        /// Used to delta-compress this command when sending it via the
        /// <see cref="CommandSendSystem{TCommandDataSerializer,TCommandData}"/>.
        /// </summary>
        /// <remarks>
        /// The default interface implementation (maintained for non-breaking change backwards compatibility) always returns 1 (has changes),
        /// so we strongly recommend overriding it in your own implementation (assuming your implementing this interface yourself).
        /// The automatic code-generated version uses a per-field change mask automatically.
        /// </remarks>
        /// <param name="snapshot">The current value.</param>
        /// <param name="baseline">The previous/baseline value.</param>
        /// <returns>A change-mask, 0 if unchanged.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        uint CalculateChangeMask(in T snapshot, in T baseline) => 1u;

        /// <summary>Helper.</summary>
        /// <returns>A short name, for use in packet dumps.</returns>
        public FixedString64Bytes ToFixedString()
        {
            var fs = new FixedString64Bytes();
            fs.CopyFromTruncated(ComponentType.ReadWrite<T>().ToFixedString()); // Ensure now overflow...
            return fs;
        }
    }

    /// <summary>
    /// Contains utility methods to add and retrieve commands from <see cref="ICommandData"/> dynamic buffers.
    /// </summary>
    public static class CommandDataUtility
    {
        /// <summary>
        /// The maximum number of commands that can be sent in one single command packet.
        /// </summary>
        public const int k_CommandDataMaxSize = 64;

        /// <summary>
        /// Get latest command data for given target tick.
        /// For example, if command buffer contains ticks 3,4,5,6 and targetTick is 5
        /// it will return tick 5 (latest without going over). If the command buffer is
        /// 1,2,3 and targetTick is 5 it will return tick 3.
        /// </summary>
        /// <param name="commandArray">Command input buffer.</param>
        /// <param name="targetTick">Target tick to fetch from.</param>
        /// <param name="commandData">The last-received input.</param>
        /// <typeparam name="T">Command input buffer type.</typeparam>
        /// <returns>Returns true if any data was found, false when no tick data is equal or older to the target tick in the buffer.</returns>
        public static bool GetDataAtTick<T>(this DynamicBuffer<T> commandArray, NetworkTick targetTick, out T commandData)
            where T : unmanaged, ICommandData
        {
            if (!targetTick.IsValid)
            {
                commandData = default;
                return false;
            }
            int beforeIdx = 0;
            NetworkTick beforeTick = NetworkTick.Invalid;
            for (int i = 0; i < commandArray.Length; ++i)
            {
                var tick = commandArray[i].Tick;
                if (tick.IsValid && !tick.IsNewerThan(targetTick) &&
                    (!beforeTick.IsValid || tick.IsNewerThan(beforeTick)))
                {
                    beforeIdx = i;
                    beforeTick = tick;
                }
            }

            if (!beforeTick.IsValid)
            {
                commandData = default(T);
                return false;
            }

            commandData = commandArray[beforeIdx];
            return true;
        }

        /// <summary>
        /// Get a readonly reference to the input at the given index. Need to be used in safe context, where you know
        /// the buffer is not going to be modified. That would invalidate the reference in that case and we can't guaratee
        /// the data you are reading is going to be valid anymore.
        /// </summary>
        /// <param name="buffer">Buffer to index</param>
        /// <param name="index">index to get input</param>
        /// <typeparam name="T">the command type</typeparam>
        /// <returns>A readonly reference to the element</returns>
        public static ref readonly T GetInputAtIndex<T>(this DynamicBuffer<T> buffer, int index) where T: unmanaged, ICommandData
        {
            return ref buffer.ElementAtRO(index);
        }

        /// <summary>
        /// Add an instance of a <see cref="ICommandData"/> into the command circular buffer.
        /// The command buffer capacity if fixed and the <see cref="ICommandData.Tick"/> is
        /// used to find in which slot the command should be put to keep the command buffer sorted.
        /// If a command with the same tick already exists in the buffer, it will be overritten
        /// </summary>
        /// <typeparam name="T">the command type</typeparam>
        /// <param name="commandBuffer">The buffer being written to.</param>
        /// <param name="commandData">The individual input struct to add.</param>
        /// <returns>True if we replaced an existing input at this exact tick value.</returns>
        public static bool AddCommandData<T>(this DynamicBuffer<T> commandBuffer, T commandData)
            where T : unmanaged, ICommandData
        {
            if (Hint.Unlikely(!commandData.Tick.IsValid))
                return false;

            var targetTick = commandData.Tick;
            int oldestIdx = 0;
            NetworkTick oldestTick = NetworkTick.Invalid;
            for (int i = 0; i < commandBuffer.Length; ++i)
            {
                var tick = commandBuffer[i].Tick;
                if (tick == targetTick)
                {
                    commandBuffer[i] = commandData;
                    return true;
                }

                if (!oldestTick.IsValid || oldestTick.IsNewerThan(tick))
                {
                    oldestIdx = i;
                    oldestTick = tick;
                }
            }

            if (commandBuffer.Length < k_CommandDataMaxSize)
                commandBuffer.Add(commandData);
            else
                commandBuffer[oldestIdx] = commandData;
            return false;
        }

        internal static FixedString64Bytes FormatBitsBytes(int sizeBits)
        {
            var bytes = (sizeBits + 7) / 8;
            return bytes <= 1 ? $"{sizeBits} bits" : $"{sizeBits} bits [{bytes} bytes]";
        }
    }
}
