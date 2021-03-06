﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Tensorflow.Keras.Utils;
using Tensorflow.Train;
using static Tensorflow.Binding;

namespace Tensorflow.Keras.Optimizers
{
    /// <summary>
    /// Updated base class for optimizers.
    /// </summary>
    public class OptimizerV2 : Trackable, IOptimizer
    {
        protected bool _hypers_created;
        protected virtual string _name { get; }

        ResourceVariable _iterations;
        List<ResourceVariable> _weight = new List<ResourceVariable>();
        Dictionary<string, float> _hyper = new Dictionary<string, float>();
        Dictionary<string, ResourceVariable> _hyper_variables = new Dictionary<string, ResourceVariable>();
        protected bool _momentum;
        protected float _initial_decay = 0.0f;

        public OptimizerV2() : base()
        {

        }

        public void apply_gradients(IEnumerable<(Tensor, ResourceVariable)> grads_and_vars)
        {
            var var_list = grads_and_vars.Select(x => x.Item2).ToArray();
            tf_with(ops.name_scope(_name), delegate
            {
                ops.init_scope();
                _create_all_weights(var_list);
                if (grads_and_vars == null || grads_and_vars.Count() == 0)
                    return control_flow_ops.no_op();

                //var apply_state = 
                _prepare(var_list);

                _aggregate_gradients(grads_and_vars);

                return null;
            });
        }

        void _aggregate_gradients(IEnumerable<(Tensor, ResourceVariable)> grads_and_vars)
        {
            var lr_t = _hyper_variables["learning_rate"];
            foreach (var grad_and_var in grads_and_vars)
            {
                var grad = grad_and_var.Item1;
                var variable = grad_and_var.Item2;
                // variable.Handle - grad * lr_t.Handle;
            }
        }

        void _prepare(ResourceVariable[] var_list)
        {
            var keys = new HashSet<(string, TF_DataType)>();
            foreach(var variable in var_list)
            {
                var lr_t = _prepare_local(variable.Device, variable.dtype.as_base_dtype());
                var momentum = _get_hyper("momentum", variable.dtype);
                array_ops.identity(momentum);
            }
        }

        ResourceVariable _prepare_local(string var_device, TF_DataType var_dtype)
        {
            var lr_t = _get_hyper("learning_rate", var_dtype);
            if(_initial_decay > 0)
            {

            }

            return lr_t;
        }

        ResourceVariable _get_hyper(string name, TF_DataType dtype = TF_DataType.DtInvalid)
        {
            var value = _hyper_variables[name];
            return math_ops.cast(value, dtype);
        }

        void _create_all_weights(ResourceVariable[] var_list)
        {
            if(_iterations == null)
            {
                _iterations = add_weight("iter", 
                    shape: new int[0], 
                    dtype: TF_DataType.TF_INT64, 
                    trainable: false, 
                    aggregation: VariableAggregation.OnlyFirstReplica);
                _weight.Add(_iterations);
            }

            _create_hypers();
            _create_slots(var_list);
        }

        protected void _set_hyper(string name, float value)
        {
            _hyper[name] = value;
        }

        void _create_hypers()
        {
            if (_hypers_created)
                return;
            foreach (var dict in _hyper)
            {
                var name = dict.Key;
                var value = dict.Value;
                _hyper_variables[name] = add_weight(
                    name,
                    shape: new int[0],
                    trainable: false,
                    initializer: tf.constant_initializer(value),
                    aggregation: VariableAggregation.OnlyFirstReplica);
            }
            _hypers_created = true;
        }

        void _create_slots(ResourceVariable[] var_list)
        {
            if(_momentum)
            {
                /*for var in var_list:
                    self.add_slot(var, "momentum")*/
            }
        }

        ResourceVariable add_weight(string name, 
            TensorShape shape, 
            TF_DataType dtype = TF_DataType.TF_FLOAT,
            IInitializer initializer = null,
            bool trainable = false,
            VariableSynchronization synchronization = VariableSynchronization.Auto,
            VariableAggregation aggregation = VariableAggregation.None)
        {
            if (initializer == null)
                initializer = tf.zeros_initializer;

            if (dtype == TF_DataType.DtInvalid)
                dtype = TF_DataType.TF_FLOAT;

            var variable = _add_variable_with_custom_getter(name: name,
                shape: shape,
                getter: base_layer_utils.make_variable,
                dtype: dtype,
                overwrite: true,
                initializer: initializer,
                trainable: trainable,
                use_resource: true,
                synchronization: synchronization,
                aggregation: aggregation);

            return variable as ResourceVariable;
        }
    }
}
