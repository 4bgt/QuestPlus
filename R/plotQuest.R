#### read in questplus Simulation data ####
setwd(dirname(rstudioapi::getActiveDocumentContext()$path)) # set working directory to source of this file
old_wd = getwd() #save this working directory

temp = list.files(pattern="\\.csv$")
data = (lapply(temp, read.delim,sep=",",dec = "."))
data = as.data.frame(do.call(rbind, data))

data$Response = as.factor(data$response)
#### Plot QuestTrials ####
##### mu, stimulus and responses over time #####
library(ggplot2)
pse_plot = ggplot(data,aes(x=trial,y=stimuli,color =Response,group = 1))+geom_line()+theme_bw()+geom_line(aes(x=trial,y=mu_estimated),color = "orange")+ylab("Stimulus")+xlab("Trial")
pse_plot

##### sigma over time #####
sigma_plot = ggplot(data,aes(x=trial,y=sigma_estimated,color =Response,group = 1))+geom_line()+theme_bw()+ylab("Sigma")+xlab("Trial")
sigma_plot

if (length(data$gamma_estimated)!= 0)
{
  ##### gamma over time #####
  gamma_plot = ggplot(data,aes(x=trial,y=gamma_estimated,color =Response,group = 1))+geom_line()+theme_bw()+ylab("Gamma")+xlab("Trial")
  gamma_plot

  ##### lambda over time #####
  lambda_plot = ggplot(data,aes(x=trial,y=lambda_estimated,color =Response,group = 1))+geom_line()+theme_bw()+ylab("Lambda")+xlab("Trial")
  lambda_plot
}else{
  saturation_plot = ggplot(data,aes(x=trial,y=saturation_estimated,color =Response,group = 1))+geom_line()+theme_bw()+ylab("Saturation")+xlab("Trial")
  saturation_plot
}

#### average responses ####
library(dplyr)
average_responses = data %>% group_by(stimuli) %>% summarize(Mean = mean(response),n = length(response))

#### plot responses and distribution ####
quest_plot = ggplot(average_responses,aes(stimuli,Mean,color=n))+geom_point()
data$mu_estimated[nrow(data)]
if (length(data$gamma_estimated) != 0)
{
  true_parameters = c(mu = pi/60,sigma= pi/120,gamma = 0.5, lambda = 0.03)
  true_distribution = data.frame(x=seq(-3,3,0.001),y=true_parameters[3]+(1-true_parameters[3]-true_parameters[4])*pnorm(seq(-3,3,0.001),true_parameters[1],true_parameters[2]))
  estimated_parameters = c(mu = data$mu_estimated[nrow(data)],sigma= data$sigma_estimated[nrow(data)] ,gamma = data$gamma_estimated[nrow(data)], lambda = data$lambda_estimated[nrow(data)])
  estimated_distribution = data.frame(x=seq(-3,3,0.001),y=estimated_parameters[3]+(1-estimated_parameters[3]-estimated_parameters[4])*pnorm(seq(-3,3,0.001),estimated_parameters[1],estimated_parameters[2]))
  
  }else{
  true_parameters = c(mu = pi/60,sigma= 0.5,saturation=0.06)
  true_distribution = data.frame(x=seq(-3,3,0.001),y=true_parameters[3]+(1-true_parameters[3]-true_parameters[3])*pnorm(seq(-3,3,0.001),true_parameters[1],true_parameters[2]))
  
  estimated_parameters = c(mu = data$mu_estimated[nrow(data)],sigma= data$sigma_estimated[nrow(data)] ,saturation = data$saturation_estimated[nrow(data)])
  estimated_distribution = data.frame(x=seq(-3,3,0.001),y=estimated_parameters[3]+(1-estimated_parameters[3]-estimated_parameters[3])*pnorm(seq(-3,3,0.001),estimated_parameters[1],estimated_parameters[2]))
  
  }
quest_plot+geom_line(data= true_distribution,aes(x,y),color = "red")+geom_line(data= estimated_distribution,aes(x,y),color = "blue") + xlab(-1,1)



#### fit psychometric function to responses (not sure if this is useful?) #### 
if (nrow(data$gamma_estimated) != 0)
{
  free_parameters = list(c(mu_min = -2 ,mu_max= 2),c(sigma_min=0,sigma_max =4),c(gamma_min=0,gamma_max=0.06),c(lambda_min=0,lambda_max=0.06))
  psychometric_fun = function(x,p) p[3] + (1 - p[3]-p[4]) *pnorm(x,p[1],p[2])
}else{
  free_parameters = list(c(mu_min = -2 ,mu_max= 2),c(sigma_min=0,sigma_max =4),c(saturation_min=0,saturation_max=0.05))
  psychometric_fun = function(x,p) p[3] + (1 - 2*p[3]) *pnorm(x,p[1],p[2])
}
fit <- quickpsy(data, stimuli, response, 
                fun = psychometric_fun, parini = free_parameters, bootstrap = "none") 
quest_plot=plot(fit)+geom_point(data = average_responses,aes(stimuli,Mean,color=n))+geom_line(data= true_distribution,aes(x,y),color = "red")
plot(quest_plot)
