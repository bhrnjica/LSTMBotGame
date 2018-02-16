using CNTK;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarioKart.Bot.NET
{

    /// <summary>
    /// class uses CNTK to train model toplay game. It takse data from the previous step in order to make model
    /// </summary>
    public class CNTKDeepNN
    {
       //training data set. (should be expanded )
        public static string trainingFilePath = "../../../../training/training_data_1.txt";

        //model name (should be created at the end of training)
        public static string cntkModel = "../../../../training/mario_kart_modelv1";

        //number of classes (movement controls for the game)
        private static readonly int numClasses = 5;

        //number of epochs for training.
        public static uint m_MinibatchSize = 64;
        public static uint m_MaxEpochs = 500000;


        static string featureName = "features";
        static string labelName = "label";

        //image size after it transformed into CHW CNTK format
        private static readonly int[] imageDim = { GenerateData.m_ResizeWidth, GenerateData.m_ResizeHeight, GenerateData.m_ResizeDepth };
        

        public static void Train(DeviceDescriptor device)
        {
            //feature dim
            var imageDims = imageDim[0] * imageDim[1] * imageDim[2];
            //stream configuration to distinct features and labels in the file
            var streamConfig = new StreamConfiguration[]
               {
                   new StreamConfiguration(featureName, imageDims),
                   new StreamConfiguration(labelName, numClasses)
               };

            // prepare the training data
            var minibatchSource = MinibatchSource.TextFormatMinibatchSource(trainingFilePath, streamConfig, m_MaxEpochs* m_MinibatchSize,true);


            //define stream info
            var imageStreamInfo = minibatchSource.StreamInfo(featureName);
            var labelStreamInfo = minibatchSource.StreamInfo(labelName);

            // build a model
            var imageInput = Variable.InputVariable(new int[] { imageDims }, DataType.Float, featureName, null, false);
            var labelsVar = Variable.InputVariable(new int[] { numClasses }, DataType.Float, labelName, new List<CNTK.Axis>() { CNTK.Axis.DefaultBatchAxis() }, false);
            //
            Vrana.NET.Lib.LSTMReccurentNN lstm = new Vrana.NET.Lib.LSTMReccurentNN(2000, 2000, device);
            var cntkMarioKartModel = lstm.CreateSequenceNet(imageInput, 7400, numClasses, "lstmMarioKart");

             // prepare for training
            var trainingLoss = CNTKLib.SquaredError(cntkMarioKartModel, labelsVar, "lossFunction");
            var prediction = CNTKLib.ClassificationError(cntkMarioKartModel, labelsVar);

            var learningRatePerSample = new TrainingParameterScheduleDouble(0.00001, 1);
            var trainer = Trainer.CreateTrainer(cntkMarioKartModel, trainingLoss, prediction,
                new List<Learner> { Learner.SGDLearner(cntkMarioKartModel.Parameters(), learningRatePerSample, new AdditionalLearningOptions() { l1RegularizationWeight=0.001, l2RegularizationWeight=0.000001 }) });

            
            int outputFrequencyInMinibatches = 20, miniBatchCount = 0;

            // Feed data to the trainer for number of epochs. 
            int counter = 0;
            while (true)
            {
                var minibatchData = minibatchSource.GetNextMinibatch(m_MinibatchSize, device);

                // Stop training once max epochs is reached.
                if (minibatchData.empty() )
                {
                    Console.WriteLine($"Epoch size before training stop {counter}");
                    break;
                }

                trainer.TrainMinibatch(new Dictionary<Variable, MinibatchData>()
                        { { imageInput, minibatchData[imageStreamInfo] },
                        { labelsVar, minibatchData[labelStreamInfo] } }, device);

                //print about progress
                PrintTrainingProgress(trainer, miniBatchCount++, outputFrequencyInMinibatches);

                counter++;
                if(counter%10000 == 0)
                    cntkMarioKartModel.Save(cntkModel+$"{counter}");
            }

            // save the model
            var imageClassifier = Function.Combine(new List<Variable>() { trainingLoss, prediction, cntkMarioKartModel }, "ImageClassifier");

            //save model version
            imageClassifier.Save(cntkModel);

        }

        public static void PrintTrainingProgress(Trainer trainer, int minibatchIdx, int outputFrequencyInMinibatches)
        {
            if ((minibatchIdx % outputFrequencyInMinibatches) == 0 && trainer.PreviousMinibatchSampleCount() != 0)
            {
                float trainLossValue = (float)trainer.PreviousMinibatchLossAverage();
                float evaluationValue = (float)trainer.PreviousMinibatchEvaluationAverage();
                Console.WriteLine($"Mini batch: {minibatchIdx} Squared Error = {trainLossValue}, Classifcation Error = {evaluationValue}");
            }

        }

    }
}
